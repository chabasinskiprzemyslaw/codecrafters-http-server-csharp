using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

Socket clientSocket = server.AcceptSocket(); // wait for client

byte[] buffer = new byte[1024];
int bufferLength = clientSocket.Receive(buffer);
string request = Encoding.UTF8.GetString(buffer, 0, bufferLength);
string[] splitRequest = request.Split("\n");

//GET / HTTP/1.1
//Host: localhost:4221
//User-Agent: curl/8.4.0
//Accept: */*

//GET / HTTP/1.1
string startLine = splitRequest[0];

string[] splitStartLine = startLine.Split(" ");

string requestPath = splitStartLine[1];

string response;
if (requestPath != "/")
{
    response = "HTTP/1.1 404 Not Found\r\n\r\n";
}
else
{
    response = "HTTP/1.1 200 OK\r\n\r\n";
}

byte[] responseBytes = Encoding.UTF8.GetBytes(response);
clientSocket.Send(responseBytes);

clientSocket.Close();
server.Stop();
