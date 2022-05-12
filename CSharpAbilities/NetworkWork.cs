using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Security;
using static CSharpAbilities.SocketManipulation;
namespace CSharpAbilities
{
#nullable disable
    public interface INetAdditional : IDisposable
    {
        ValueTask DownloadFileAsync(string uri, string to);
        ValueTask<IPAddress[]> ResolveHostAsync(string domainName);
        ValueTask GetNetworkStateAsync();
    }
    public interface IConnectionSetuper
    {
        int DefaultConnectionLimit { get; }
        int DnsRefreshTimeout { get; }
        ValueTask FormatInfo();
        IConnectionSetuper ChangeSettings(int defaultConnectionLimit, int dnsRefreshTimeout);
    }
    public delegate Task HandleRequest(HttpListenerContext ctx);
    public interface IHttpListener : IDisposable
    {
        event HandleRequest RequestCaptured;
        ValueTask ListenAndHandle();
    }
    public interface ITcpConnector : IDisposable
    {
        bool Bind(IPEndPoint localPoint);
        bool ConnectAsServer(int maximumClients, byte[] serverData, out byte[] clientData);
        bool ConnectAsClient(IPEndPoint serverPoint, byte[] clientData, out byte[] serverData);
    }
    public interface IUdpConnector : IDisposable
    {
        bool Bind(IPEndPoint serverPoint);
        bool SendTo(IPEndPoint serverPoint, byte[] data, out byte[] serverData);
        bool ListenFrom(byte[] serverData, out byte[] data);
        bool JoinGroup(IPAddress groupAddress,byte[] answer, out byte[] data);
    }
    public interface IHttpConnector : IDisposable
    {
        IHttpConnector SetDecompression(DecompressionMethods methods);
        IHttpConnector SetProxy(string host, int port, NetworkCredential credentials = null);
        IHttpConnector SetBaseAddress(string baseAddress);
        IHttpConnector BuildConnector();
        ValueTask<string> GetStringAsync(string addingRoute, bool onlyHeaders = false);
        ValueTask<Stream> GetStreamAsync(string addingRoute, bool onlyHeaders = false);
        ValueTask<byte[]> GetByteArrayAsync(string addingRoute, bool onlyHeaders = false);
        ValueTask<bool> PostJsonAsync(string addingRoute, string json);
        ValueTask<bool> PostByteArrayAsync(string addingRoute, byte[] data);
        ValueTask<bool> PostStreamAsync(string addingRoute, Stream stream);
        ValueTask<bool> PutJsonAsync(string addingRoute, string json);
        ValueTask<bool> PutByteArrayAsync(string addingRoute, byte[] data);
        ValueTask<bool> PutStreamAsync(string addingRoute, Stream stream);
        ValueTask<bool> DeleteFromAsync(string addingRoute);
    }
    public struct NetAdditional : INetAdditional
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        private readonly IConnectionSetuper _setuper;
        private readonly WebClient _client;
        public async ValueTask DownloadFileAsync(string uri, string physicalPath)
        {
            await _client?.DownloadFileTaskAsync(new Uri(uri), physicalPath);
        }
        public async ValueTask<IPAddress[]> ResolveHostAsync(string domainName)
        {
            try
            {
                return (await Dns.GetHostEntryAsync(domainName)).AddressList;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["DnsResolverError"]);
                return null;
            }
        }
        public async ValueTask GetNetworkStateAsync()
        {
            try
            {
                await _setuper.FormatInfo();
                await _printer.PrintAsync(string.Concat(IPAddress.Loopback.MapToIPv4().ToString(), Dns.GetHostName()));
            }
            catch
            {
                await _printer.PrintAsync(_localizer["DnsResolverError"]);
            }
        }
        public void Dispose()
        {
            _client.Dispose();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
    }
    public struct StandardHttpListener : IHttpListener
    {
        public event HandleRequest RequestCaptured;
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        private readonly HttpListener _listener;
        public StandardHttpListener(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
            _listener = new HttpListener();
            RequestCaptured = async (request) => { };
        }
        public async ValueTask ListenAndHandle()
        {
            try
            {
                _listener.Start();
                HttpListenerContext ctx = await _listener.GetContextAsync();
                await RequestCaptured?.Invoke(ctx);
                _listener.Stop();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpListenerError"]);
            }
        }
        public void Dispose()
        {
            _listener.Close();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
    }
    public class HttpConnector : IHttpConnector
    {
        private readonly HttpClientHandler _handler = new HttpClientHandler();
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        private HttpClient _client;
        public HttpConnector(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
        }
        public void Dispose()
        {
            _client.Dispose();
            _client = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
        public IHttpConnector SetDecompression(DecompressionMethods methods)
        {
            _handler.AutomaticDecompression = methods;
            return this;
        }
        public IHttpConnector SetProxy(string host, int port, NetworkCredential credential = null)
        {
            _handler.Proxy = new WebProxy(host, port) { Credentials = credential };
            return this;
        }
        public IHttpConnector SetBaseAddress(string uri)
        {
            if (_client is not null)
            {
                _client.BaseAddress = new Uri(uri);
            }
            return this;
        }
        public IHttpConnector BuildConnector()
        {
            _client = new HttpClient(_handler);
            return this;
        }
        public async ValueTask<bool> DeleteFromAsync(string addingRoute)
        {
            try
            {
                var content = await _client.DeleteAsync(addingRoute);
                await _printer.PrintAsync(await content.Content.ReadAsStringAsync());
                return content.IsSuccessStatusCode;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                return false;
            }
        }
        public async ValueTask<bool> PostByteArrayAsync(string addingRoute, byte[] data)
        {
            try
            {
                var content = await _client.PostAsync(addingRoute, new ByteArrayContent(data));
                await _printer.PrintAsync(await content.Content.ReadAsStringAsync());
                return content.IsSuccessStatusCode;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return false;
            }
        }
        public async ValueTask<bool> PutStreamAsync(string addingRoute, Stream stream)
        {
            try
            {
                var content = await _client.PutAsync(addingRoute, new StreamContent(stream));
                await _printer.PrintAsync(await content.Content.ReadAsStringAsync());
                return content.IsSuccessStatusCode;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return false;
            }
        }
        public async ValueTask<bool> PutJsonAsync(string addingRoute, string json)
        {
            try
            {
                var content = await _client.PutAsync(addingRoute, new StringContent(json, Encoding.Default, "application/json"));
                await _printer.PrintAsync(await content.Content.ReadAsStringAsync());
                return content.IsSuccessStatusCode;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return false;
            }
        }
        public async ValueTask<bool> PutByteArrayAsync(string addingRoute, byte[] data)
        {
            try
            {
                var content = await _client.PutAsync(addingRoute, new ByteArrayContent(data));
                await _printer.PrintAsync(await content.Content.ReadAsStringAsync());
                return content.IsSuccessStatusCode;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return false;
            }
        }
        public async ValueTask<bool> PostStreamAsync(string addingRoute, Stream stream)
        {
            try
            {
                var content = await _client.PostAsync(addingRoute, new StreamContent(stream));
                await _printer.PrintAsync(await content.Content.ReadAsStringAsync());
                return content.IsSuccessStatusCode;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return false;
            }
        }
        public async ValueTask<bool> PostJsonAsync(string addingRoute, string json)
        {
            try
            {
                var content = await _client.PostAsync(addingRoute, new StringContent(json, Encoding.Default, "application/json"));
                await _printer.PrintAsync(await content.Content.ReadAsStringAsync());
                return content.IsSuccessStatusCode;
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return false;
            }
        }
        public async ValueTask<string> GetStringAsync(string addingRoute, bool onlyHeaders = false)
        {
            try
            {
                return await (await _client.GetAsync(addingRoute, onlyHeaders ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead)).Content.ReadAsStringAsync();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return _localizer["HttpConnectorError"];
            }
        }
        public async ValueTask<Stream> GetStreamAsync(string addingRoute, bool onlyHeaders = false)
        {
            try
            {
                return await (await _client.GetAsync(addingRoute, onlyHeaders ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead)).Content.ReadAsStreamAsync();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                var stream = File.Create(Path.GetTempFileName());
                await stream.WriteAsync(Encoding.Default.GetBytes(_localizer["HttpConnectorError"]));
                return stream;
            }
        }
        public async ValueTask<byte[]> GetByteArrayAsync(string addingRoute, bool onlyHeaders = false)
        {
            try
            {
                return await (await _client.GetAsync(addingRoute, onlyHeaders ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead)).Content.ReadAsByteArrayAsync();
            }
            catch
            {
                await _printer.PrintAsync(_localizer["HttpConnectorError"]);
                _client?.CancelPendingRequests();
                return Encoding.Default.GetBytes(_localizer["HttpConnectorError"]);
            }
        }
    }
    public readonly struct UdpConnector : IUdpConnector
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        private readonly Socket _socket;
        private readonly UdpClient _client;
        public UdpConnector(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
            _client = new UdpClient() { MulticastLoopback = false };
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }
        public readonly bool JoinGroup(IPAddress address,byte[] answer,out byte[] data)
        {
            try
            {
                _client.JoinMulticastGroup(address);
                IPEndPoint point = new IPEndPoint(IPAddress.Any, 0);
                data = _client.Receive(ref point);
                _client.Send(answer, answer.Length, point);
                return true;
            }
            catch
            {
                data = null;
                return false;
            }
        }
        
        public readonly void Dispose()
        {
            StopSocket(_socket);
            _client.Dispose();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
        public readonly bool Bind(IPEndPoint point)
        {
            try
            {
                _socket.Bind(point);
                return true;
            }
            catch
            {
                _printer.Print(_localizer["SocketBindError"]);
                return false;
            }
        }
        public bool SendTo(IPEndPoint serverPoint, byte[] dataTo, out byte[] dataFrom)
        {
            try
            {
                _socket.SendTo(dataTo, serverPoint);
                dataFrom = ReceiveData(_socket);
                return true;
            }
            catch
            {
                _printer.Print(_localizer["UdpClientError"]);
                dataFrom = Encoding.Default.GetBytes(_localizer["UdpClientError"]);
                return false;
            }
        }
        public bool ListenFrom(byte[] serverResponse, out byte[] clientData)
        {
            try
            {
                Span<byte> span = new Span<byte>(new byte[4096]);
                EndPoint point = new IPEndPoint(IPAddress.Any, 0);
                _socket.ReceiveFrom(span, ref point);
                _socket.SendTo(serverResponse, point);
                clientData = span.ToArray();
                return true;
            }
            catch
            {
                _printer.Print(_localizer["UdpServerError"]);
                clientData = Encoding.Default.GetBytes(_localizer["UdpServerError"]);
                return false;
            }
        }
    }
    public readonly struct TcpConnector : ITcpConnector
    {
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        private readonly Socket _socket;
        public TcpConnector(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            _printer = printer;
            _localizer = localizer;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public readonly bool Bind(IPEndPoint point)
        {
            try
            {
                _socket.Bind(point);
                return true;
            }
            catch
            {
                _printer.Print(_localizer["SocketBindError"]);
                return false;
            }
        }
        public readonly bool ConnectAsServer(int maxClients, byte[] serverData, out byte[] clientData)
        {
            try
            {
                _socket.Listen(maxClients);
                Socket accepter = _socket.Accept();
                clientData = ReceiveData(accepter);
                accepter.Send(serverData);
                StopSocket(_socket);
                return true;
            }
            catch
            {
                _printer.Print(_localizer["SocketServerError"]);
                clientData = Encoding.Default.GetBytes(_localizer["SocketServerError"]);
                return false;
            }
        }
        public readonly bool ConnectAsClient(IPEndPoint point, byte[] clientData, out byte[] serverData)
        {
            try
            {
                _socket.Send(clientData);
                serverData = ReceiveData(_socket);
                return true;
            }
            catch
            {
                _printer.Print(_localizer["SocketClientError"]);
                serverData = Encoding.Default.GetBytes(_localizer["SocketClientError"]);
                return false;
            }
        }
        public readonly void Dispose()
        {
            StopSocket(_socket);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }

    }
    public readonly struct ConnectionSetuper : IConnectionSetuper
    {
        public int DefaultConnectionLimit { get; }
        public int DnsRefreshTimeout { get; }
        private readonly IPrinter<string> _printer;
        private readonly ILocalizer<string> _localizer;
        public ConnectionSetuper(IPrinter<string> printer, ILocalizer<string> localizer)
        {
            DefaultConnectionLimit = ServicePointManager.DefaultConnectionLimit;
            DnsRefreshTimeout = ServicePointManager.DnsRefreshTimeout;
            _localizer = localizer;
            _printer = printer;
        }
        public readonly IConnectionSetuper ChangeSettings(int defaultConnectionLimit, int dnsRefreshTimeout)
        {
            ServicePointManager.DefaultConnectionLimit = defaultConnectionLimit;
            ServicePointManager.DnsRefreshTimeout = dnsRefreshTimeout;
            return new ConnectionSetuper(_printer, _localizer);
        }
        public readonly async ValueTask FormatInfo()
        {
            await _printer.PrintAsync(String.Concat(_localizer["DefaultConnectionLimit"], DefaultConnectionLimit,
                _localizer["DnsRefreshTimeout"], DnsRefreshTimeout));
        }
    }
    public static class SocketManipulation
    {
        public static byte[] ReceiveData(Socket from)
        {
            List<byte> dataFrom = new List<byte>(512);
            while (from.Available > 0)
            {
                Span<byte> temp = new Span<byte>(new byte[from.Available]);
                from.Receive(temp, SocketFlags.None);
                dataFrom.AddRange(temp.ToArray());
            }
            return dataFrom.ToArray();
        }
        public static void StopSocket(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
