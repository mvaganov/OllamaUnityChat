// Select Project > Manage NuGet Packages.
// In the NuGet Package Manager page, choose nuget.org as the Package source. (upper right)
// From the Browse tab, search for Newtonsoft.Json, select Newtonsoft.Json in the list, and then select Install.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class OllamaInterface {
	private const int PORT = 11434;
	private const string HOST = "localhost";
	public TcpClient client;

	static void Main(string[] args) {
		Console.WriteLine("Enter your prompt (or 'exit' to quit):");
		OllamaInterface ollamaInterface = new OllamaInterface();
		while (true) {
			string input = Console.ReadLine();
			if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
				break;
			Task task = ollamaInterface.Ask(input, "deepseek-r1:7b", Console.Write, () => { Console.WriteLine("\n"); }, Console.Error.WriteLine);
			if (!task.IsCompleted) { Console.Write("."); Thread.Sleep(1); }
		}
	}

	public async Task Ask(string question, string model, Action<string> onResponse, Action onFinish, Action<string> onError) {
		try {
			client = await SendQuestionToOllama(question, model, onError);
			if (client == null) { return; }
			NetworkStream stream = client.GetStream();
			await ProcessResponse(stream, onResponse, onError);
			client.Dispose();
		} catch (Exception ex) {
			onError?.Invoke($"{ex.Message}\n{ex.StackTrace}");
		}
		onFinish?.Invoke();
	}

	private static async Task<TcpClient> SendQuestionToOllama(string input, string model, Action<string> onError) {
		TcpClient client = new TcpClient();
		try {
			await client.ConnectAsync(HOST, PORT);
			NetworkStream stream = client.GetStream();

			// Create the request JSON
			var request = new {
				model = model,
				prompt = input,
				stream = true
			};
			string jsonRequest = JsonConvert.SerializeObject(request, Formatting.None);

			// Prepare the HTTP request
			string httpRequest =
				$"POST /api/generate HTTP/1.1\r\n" +
				$"Host: {HOST}:{PORT}\r\n" +
				"Content-Type: application/json\r\n" +
				$"Content-Length: {Encoding.UTF8.GetByteCount(jsonRequest)}\r\n" +
				"\r\n" +
				jsonRequest;

			// Send the request
			byte[] requestBytes = Encoding.UTF8.GetBytes(httpRequest);
			await Task.Yield();
			//await stream.WriteAsync(requestBytes);
			ValueTask streamTask = stream.WriteAsync(requestBytes);
			while (!streamTask.IsCompleted) {
				await Task.Yield();
			}
		} catch (Exception ex) {
			onError?.Invoke($"{ex.Message}\n{ex.StackTrace}");
			client.Dispose();
			return null;
		}
		return client;
	}

	private static async Task ProcessResponse(NetworkStream stream, Action<string> onResponse, Action<string> onError) {
		byte[] buffer = new byte[4096];
		bool headersParsed = false;
		string unprocessedChunk = null;
		while (true) {
			await Task.Yield();
			//int bytesRead = await stream.ReadAsync(buffer);
			ValueTask<int> bytesReadTask = stream.ReadAsync(buffer);
			int bytesRead = 0;
			while (!bytesReadTask.IsCompleted) {
				await Task.Yield();
			}
			bytesRead = bytesReadTask.Result;
			if (bytesRead == 0) break;

			string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			if (unprocessedChunk != null) {
				chunk = unprocessedChunk + chunk;
			}

			if (IgnoreChunksUntilHeaderIsFinished(ref chunk, ref unprocessedChunk, ref headersParsed)) {
				continue;
			}

			if (ChunkContainsIncompleteJsonObject(chunk)) {
				unprocessedChunk = chunk;
			} else if (ProcessEachlineInTheResponse(chunk, onResponse, onError) == ParseState.Finished) {
				unprocessedChunk = null;
				break;
			}
		}
	}

	private static bool IgnoreChunksUntilHeaderIsFinished(ref string chunk, ref string unprocessedChunk, ref bool headersParsed) {
		if (headersParsed) { return false; }
		int headerEnd = chunk.IndexOf("\r\n\r\n");
		if (headerEnd >= 0) {
			chunk = chunk.Substring(headerEnd + 4);
			unprocessedChunk = null;
			headersParsed = true;
			return false;
		}
		unprocessedChunk = chunk;
		return true;
	}

	private static bool ChunkContainsIncompleteJsonObject(string chunk) {
		return chunk.Contains("{") && !chunk.Contains("}");
	}

	public enum ParseState { Undefined, Parsing, SkippedNonJsonObject, Error, Finished }

	private static ParseState ProcessEachlineInTheResponse(string chunk, Action<string> onResponse, Action<string> onError) {
		ParseState parseState = ParseState.Undefined;
		string[] lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		foreach (string line in lines) {
			string fixedLine = line.Replace("\r", "");
			if (fixedLine.Length == 0 ||
			(fixedLine.Length <= 8 && int.TryParse(fixedLine, NumberStyles.HexNumber, null, out int result))) { continue; }
			try {
				parseState = ParseResponseTextFromResponseJson(fixedLine, onResponse, onError);
			} catch (Exception ex) {
				onError?.Invoke($"'{fixedLine}'\n{ex.Message}\n{ex.StackTrace}");
			}
			if (parseState == ParseState.Finished) {
				break;
			}
		}
		return parseState;
	}

	private static ParseState ParseResponseTextFromResponseJson(string responseText, Action<string> onResponse, Action<string> onError) {
		JObject response = JsonConvert.DeserializeObject<JObject>(responseText);
		if (response.Type != JTokenType.Object) {
			return ParseState.SkippedNonJsonObject;
		}
		if (response.TryGetValue("error", out JToken error)) {
			onError?.Invoke($"{error.ToString()}\n{responseText}");
			return ParseState.Error;
		}
		if (response.ContainsKey("done") && response.Value<bool>("done")) {
			return ParseState.Finished;
		}
		if (response.TryGetValue("response", out JToken content)) {
			onResponse?.Invoke(content.ToString());
			return ParseState.Parsing;
		}
		onError?.Invoke($"unable to parse \"{responseText}\"");
		return ParseState.Undefined;
	}
}
