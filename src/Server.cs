using System.Net;
using System.Net.Sockets;
using System.Text;

const string HTTP_VERSION = "HTTP/1.1";
const string HTTP200OK = "200 OK";
const string HTTP404NotFound = "404 Not Found";
const string HTTP201Created = "201 Created";

string directory = parseDirectory(args);

Console.WriteLine("Starting server...");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
Console.WriteLine("Listening on port 4221...");
server.Start();

while (true) {
    Socket clientSocket = await server.AcceptSocketAsync(); // wait for client
    await HandeRequest(clientSocket);
}

async Task HandeRequest(Socket socket) {
    byte[] buffer = new byte[1024];
    int bufferLength = await socket.ReceiveAsync(buffer);
    string request = Encoding.UTF8.GetString(buffer, 0, bufferLength);
    string acceptEncoding = extractHeader(request.Split("\n"), "Accept-Encoding:");
    string httpMethod = extractHttpMethod(request);
    string response = createResponse(httpMethod, acceptEncoding, request);

    byte[] responseBytes = Encoding.UTF8.GetBytes(response);

    await socket.SendAsync(responseBytes);

    socket.Close();
}

string createResponse(string httpMethod, string acceptEncoding, string request) {
    string responseContent = string.Empty;
    if (httpMethod.StartsWith("GET")) {
        responseContent = createGetResponse(acceptEncoding, request);
    }
    else if (httpMethod.StartsWith("POST")) {
        responseContent = handlePostRequest(request);
    }
    else {
        responseContent = $"{HTTP_VERSION} {HTTP404NotFound}\r\n\r\n";
    }
    return responseContent;
}

string handlePostRequest(string request) {
    string responseContent = string.Empty;
    string requestPath = extractPath(request);

    if (requestPath.StartsWith("/files")) {
        string fileName = extractParamFromPath(requestPath);
        string fileContent = request.Split("\n")[request.Split("\n").Length - 1];
        File.WriteAllText($"{directory}{fileName}", fileContent);
        responseContent = $"{HTTP_VERSION} {HTTP201Created}\r\n\r\n";
    }
    else {
        responseContent = $"{HTTP_VERSION} {HTTP404NotFound}\r\n\r\n";
    }

    return responseContent;
}

string createGetResponse(string acceptEncoding, string request) {
    string responseContent = string.Empty;
    string requestPath = extractPath(request);
    string[] splitRequest = request.Split("\n");
    if (requestPath is "/") {
        responseContent = $"{HTTP_VERSION} {HTTP200OK}\r\n\r\n";
    }
    else if (requestPath.StartsWith("/user-agent")) {
        string userAgent = extractHeader(splitRequest, "User-Agent:");
        if (!string.IsNullOrEmpty(userAgent)) {
            responseContent = buildResponse(userAgent, acceptEncoding);
        }
        else {
            responseContent = $"{HTTP_VERSION} {HTTP404NotFound}\r\n\r\n";
        }
    }
    else if (requestPath.StartsWith("/echo")) {
        string param = extractParamFromPath(requestPath);
        responseContent = buildResponse(param, acceptEncoding);
    }
    else if (requestPath.StartsWith("/files")) {
        string fileName = extractParamFromPath(requestPath);
        try {
            string fileContent = File.ReadAllText($"{directory}{fileName}");
            responseContent = buildResponse(fileContent, acceptEncoding, true);
        }
        catch (FileNotFoundException) {
            responseContent = $"{HTTP_VERSION} {HTTP404NotFound}\r\n\r\n";
        }
    }
    else {
        responseContent = $"{HTTP_VERSION} {HTTP404NotFound}\r\n\r\n";
    }

    return responseContent;
}

string parseDirectory(string[] args) {
    string directory = string.Empty;
    if (args.Length > 0) {
        directory = args[1];
    }
    return directory;
}

string extractHeader(string[] splitRequest, string headerName) {
    string headerValue = string.Empty;
    foreach (var header in splitRequest) {
        if (header.StartsWith(headerName)) {
            headerValue = header.Substring(headerName.Length).Trim();
            break;
        }
    }
    return headerValue;
}

string buildResponse(string responseContent, string acceptEncoding, bool isFile = false) {
    int responseBodyBytes = Encoding.ASCII.GetByteCount(responseContent);
    string contentType = isFile ? "application/octet-stream" : "text/plain";
    string contentEncoding = $"Content-Encoding: {acceptEncoding}\r\n";
    string contentTypeHeader = $"Content-Type: {contentType}\r\n";
    string contentLength = $"Content-Length: {responseBodyBytes}\r\n\r\n";

    StringBuilder sb = new StringBuilder();
    string startLine = $"{HTTP_VERSION} {HTTP200OK}\r\n";
    sb.Append(startLine);
    if (acceptEncoding.Contains("gzip")) {
        sb.Append(contentEncoding);
    }
    sb.Append(contentTypeHeader);
    sb.Append($"{contentLength}");
    sb.Append(responseContent);
    return sb.ToString();
}

string extractHttpMethod(string request) {
    string[] splitRequest = request.Split("\n");
    string startLine = splitRequest[0];
    string[] splitStartLine = startLine.Split(" ");
    return splitStartLine[0];
}

string extractPath(string request) {
    string[] splitRequest = request.Split("\n");
    string startLine = splitRequest[0];
    string[] splitStartLine = startLine.Split(" ");
    
    //GET / HTTP/1.1 
    //or GET /user-agent HTTP/1.1
    //GET /echo/abc HTTP/1.1
    return splitStartLine[1];
}

string extractParamFromPath(string path) {
    string[] splitPath = path.Split("/");
    return splitPath[2];
}


//GET 
// / 
//HTTP/1.1
//Host: localhost:4221
//User-Agent: curl/8.4.0
//Accept: */*

//GET / HTTP/1.1