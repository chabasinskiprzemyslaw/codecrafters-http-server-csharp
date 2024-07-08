using System.Net;
using System.Net.Sockets;
using System.Text;

const string HTTP_VERSION = "HTTP/1.1";
const string HTTP200OK = "200 OK";
const string HTTP404NotFound = "404 Not Found";

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
    string response = createResponse(request);

    byte[] responseBytes = Encoding.UTF8.GetBytes(response);

    await socket.SendAsync(responseBytes);

    socket.Close();
}

string createResponse(string request) {
    string responseContent = string.Empty;
    string requestPath = extractPath(request);
    string[] splitRequest = request.Split("\n");
    if (requestPath is "/") {
        responseContent = $"{HTTP_VERSION} {HTTP200OK}\r\n\r\n";
    }
    else if (requestPath.StartsWith("/user-agent")) {
        string userAgent = extractHeader(splitRequest, "User-Agent:");
        if (!string.IsNullOrEmpty(userAgent)) {
            responseContent = buildResponse(userAgent);
        }
        else {
            responseContent = $"{HTTP_VERSION} {HTTP404NotFound}\r\n\r\n";
        }
    }
    else if (requestPath.StartsWith("/echo")) {
        string param = extractParamFromPath(requestPath);
        responseContent = buildResponse(param);
    }
    else if (requestPath.StartsWith("/files")) {
        string fileName = extractParamFromPath(requestPath);
        try {
            string fileContent = File.ReadAllText($"{directory}{fileName}");
            responseContent = buildResponse(fileContent, true);
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

string buildResponse(string responseContent, bool isFile = false) {
    int responseBodyBytes = Encoding.ASCII.GetByteCount(responseContent);
    string contentType = isFile ? "application/octet-stream" : "text/plain";
    string responseHeaders = $"{HTTP_VERSION} {HTTP200OK}\r\nContent-Type: {contentType}\r\nContent-Length: {responseBodyBytes}\r\n\r\n{responseContent}";
    return responseHeaders;
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