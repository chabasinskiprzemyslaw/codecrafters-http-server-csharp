using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

Socket clientSocket = server.AcceptSocket(); // wait for client

string response = "HTTP/1.1 200 OK\r\n\r\n";
byte[] responseBytes = Encoding.UTF8.GetBytes(response);
clientSocket.Send(responseBytes);

clientSocket.Close();
server.Stop();
