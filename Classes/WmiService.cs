using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace ScriptRunner
{
    public static class WmiService
    {
        // Your other methods (IsSubnet, ExpandCidr, PingHost, IsPortOpenAsync) are fine.
        // ...

        public static async Task<List<ManagementObject>> RunWmiQueryWithTimeout(
            string target,
            string ns,
            string query,
            int timeoutMs,
            ConnectionOptions options)
        {
            var resultCollection = new List<ManagementObject>();

            try
            {
                // Create a CancellationTokenSource for a more reliable timeout.
                using (var cts = new System.Threading.CancellationTokenSource(timeoutMs))
                {
                    var queryTask = Task.Run(() =>
                    {
                        try
                        {
                            var scope = new ManagementScope($"\\\\{target}\\{ns}", options);
                            scope.Connect();
                            var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
                            using (var results = searcher.Get())
                            {
                                // Iterate and clone the objects immediately.
                                foreach (ManagementObject obj in results)
                                {
                                    resultCollection.Add((ManagementObject)obj.Clone());
                                    // Throw an exception if the cancellation token is signaled.
                                    // This handles a timeout during the enumeration.
                                    cts.Token.ThrowIfCancellationRequested();
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw; // Re-throw the cancellation exception.
                        }
                        catch (ManagementException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"WMI Management Exception: {ex.Message} (ErrorCode: {ex.ErrorCode})");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"WMI query failed: {ex.Message}");
                            throw;
                        }
                    }, cts.Token);

                    await queryTask;
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"WMI query to {target}\\{ns} timed out or was canceled.");
                return new List<ManagementObject>(); // Return empty list on timeout.
            }
            catch
            {
                return new List<ManagementObject>(); // Return empty list on any other error.
            }

            return resultCollection;
        }


        /*
         * using System;

        */

        public static bool IsSubnet(string input)
        {
            return input.Contains("/") && IPAddress.TryParse(input.Split('/')[0], out _);
        }

        public static IEnumerable<string> ExpandCidr(string cidr)
        {
            var parts = cidr.Split('/');
            var baseIp = IPAddress.Parse(parts[0]);
            int prefix = int.Parse(parts[1]);
            int max = 32 - prefix;
            uint ip = BitConverter.ToUInt32(baseIp.GetAddressBytes().Reverse().ToArray(), 0);
            uint count = (uint)(1 << max);
            for (uint i = 1; i < count - 1; i++)
            {
                var bytes = BitConverter.GetBytes(ip + i).Reverse().ToArray();
                yield return new IPAddress(bytes).ToString();
            }
        }

        public static async Task<bool> PingHost(string host)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(host, 500);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> IsPortOpenAsync(string host, int port, int timeout = 1000)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(timeout);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    return completedTask == connectTask && client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

        /*     
             public static async Task<ICollection> RunWmiQueryWithTimeout(string target, string ns, string query, int timeoutMs, ConnectionOptions options)
         {
             var task = Task.Run(() =>
             {
                 try
                 {
                     System.Diagnostics.Debug.WriteLine($"Connecting to WMI on {target} namespace {ns}");

                     var scope = new ManagementScope($"\\\\{target}\\{ns}", options);
                     scope.Connect();

                     System.Diagnostics.Debug.WriteLine($"WMI connection successful, executing query: {query}");

                     ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
                     ManagementObjectCollection results = searcher.Get();
                     System.Diagnostics.Debug.WriteLine($"WMI query executed, result count: {results.Count}");

                     return results;
                 }
                 catch (ManagementException mex)
                 {
                     System.Diagnostics.Debug.WriteLine($"WMI Management Exception: {mex.Message} (ErrorCode: {mex.ErrorCode})");
                     return new ManagementObjectCollection(new ManagementObjectCollection.ManagementObjectEnumerator()); // Empty collection
                 }
                 catch (UnauthorizedAccessException uex)
                 {
                     System.Diagnostics.Debug.WriteLine($"WMI Access Denied: {uex.Message}");
                     return new ManagementObjectCollection
                 }
                 catch (Exception ex)
                 {
                     System.Diagnostics.Debug.WriteLine($"WMI query failed: {ex.Message}");
                     return  Collection<ManagementObjectCollection> (new ManagementObjectCollection.ManagementObjectEnumerator()); // Empty collection
                 }
             });

             var timeoutTask = Task.Delay(timeoutMs);

             var completedTask = await Task.WhenAny(task, timeoutTask);

             if (completedTask == timeoutTask)
             {
                 System.Diagnostics.Debug.WriteLine($"WMI query timed out after {timeoutMs}ms");
                 // Don't cancel the task, but return an empty collection if it wasn't successful
                 if (!task.IsCompleted)
                 {
                     return new ManagementObjectCollection(new ManagementObjectCollection.ManagementObjectEnumerator());
                 }
             }

             try
             {
                 return await task;
             }
             catch
             {
                 // Final safety net in case the task throws an exception
                 return new ManagementObjectCollection collection = (new ManagementObjectCollection.ManagementObjectEnumerator());
             }
         }
     }
 }
 */

    