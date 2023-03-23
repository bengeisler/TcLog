using System;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Threading;

namespace TcLogTest.NET
{
    [Collection("Plc collection")]
    public class PersistingToFileSystem : IDisposable
    {
        PlcFixture fixture;
        readonly string mut = "MAIN.testWrapper";
        readonly string path = "C:\\UnitTest\\";
        readonly string filename = "UnitTest.txt";
        readonly uint hPath;
        readonly uint hFileName;

        readonly uint hSystemTime;
        readonly uint hSetSystemTime;
        readonly uint hTime; 

        public PersistingToFileSystem(PlcFixture fixture)
        {
            this.fixture = fixture;

            hPath = fixture.TcClient.CreateVariableHandle(mut + ".filePath");
            hFileName = fixture.TcClient.CreateVariableHandle(mut + ".fileName");
            fixture.TcClient.WriteAny(hPath, path);
            fixture.TcClient.WriteAny(hFileName, filename);

            Directory.CreateDirectory(path);
            var files = Directory.GetFiles(path);
            foreach (var f in files) File.Delete(f);

            hSystemTime = fixture.TcClient.CreateVariableHandle(mut + ".System_time");
            hSetSystemTime = fixture.TcClient.CreateVariableHandle(mut + ".Set_system_time");
            hTime = fixture.TcClient.CreateVariableHandle(mut + ".ActualTime");
        }

        public void Dispose()
        {
            fixture.TcClient.DeleteVariableHandle(hPath);
            fixture.TcClient.DeleteVariableHandle(hFileName);
            fixture.TcClient.DeleteVariableHandle(hSystemTime);
            fixture.TcClient.DeleteVariableHandle(hSetSystemTime);
            fixture.TcClient.DeleteVariableHandle(hTime);
        }

        private async Task SetTime(string setTime, string testTime)
        {
            fixture.TcClient.WriteAny(hSetSystemTime, false);
            fixture.TcClient.WriteAny(hSystemTime, setTime);
            fixture.TcClient.WriteAny(hSetSystemTime, true);
            while (fixture.TcClient.ReadAnyString(hTime, 80, System.Text.Encoding.ASCII).Contains(testTime) != true)
                await Task.Delay(200);
            fixture.TcClient.WriteAny(hSetSystemTime, false);
        }

        [Fact]
        public async void Persist_simple_error_message()
        {          
            string message = "Simple message";  
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Persist_simple_error_message_run");
            uint hData = fixture.TcClient.CreateVariableHandle(mut + ".Persist_simple_error_message_data");
            
            fixture.TcClient.WriteAny(hData, message);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllLines(files[0]);

            Assert.Contains(message, fileContent[0]);
            Assert.Contains("Error", fileContent[0]);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hData);
        }

        [Fact]
        public async void Persist_long_error_message()
        {
            string message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aenean aliquet orci sit amet massa placerat faucibus. Sed interdum fermentum eros. Maecenas accumsan rutrum ex, non varius orci scelerisque ac. Donec quis venenatis sem, sit amet congue orci tellus.";
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Persist_long_error_message_run");
            uint hData = fixture.TcClient.CreateVariableHandle(mut + ".Persist_long_error_message_data");

            fixture.TcClient.WriteAny(hData, message);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllLines(files[0]);

            Assert.Contains(message, fileContent[0]);
            Assert.Contains("Error", fileContent[0]);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hData);
        }

        [Fact]
        public async void Do_not_persist_logs_below_log_level()
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Do_not_persist_logs_below_log_level_run");

            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);

            Assert.Empty(files);

            fixture.TcClient.DeleteVariableHandle(hRun);
        }

        [Fact]
        public async void Log_message_contains_instance_path()
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Log_message_contains_instance_path_run");

            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllLines(files[0]);

            Assert.Contains(mut, fileContent[0]);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
        }

        [Fact]
        public async void Log_message_uses_correct_delimiter()
        {
            string message = "Simple message";
            string delimiter = ";";
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Log_message_uses_correct_delimiter_run");
            uint hData = fixture.TcClient.CreateVariableHandle(mut + ".Log_message_uses_correct_delimiter_data");
            uint hDelimiter = fixture.TcClient.CreateVariableHandle(mut + ".delimiter");

            fixture.TcClient.WriteAny(hDelimiter, delimiter);
            fixture.TcClient.WriteAny(hData, message);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllLines(files[0]);

            Assert.Contains(delimiter + message, fileContent[0]);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hData);
            fixture.TcClient.DeleteVariableHandle(hDelimiter);
        }

        [Fact]
        public async void Log_message_contains_custom_formatted_timestamp()
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Log_message_contains_custom_formatted_timestamp_run");
            fixture.TcClient.WriteAny(hRun, false);

            await SetTime("2021-08-17-22:05:01.300", "2021-08-17-22:05:");
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllLines(files[0]);

            Assert.Contains("2021-08-17-22:05:", fileContent[0]);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
        }

        [Fact]
        public async void Delete_logs_if_expired()
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Delete_logs_if_expired_run");
            fixture.TcClient.WriteAny(hRun, false);
            var files = Directory.GetFiles(path);
            foreach (var f in files) File.Delete(f);

            await SetTime("2021-08-17-21:59:58.300", "2021-08-17-21:59:");
            await Task.Delay(3000);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(2000);
            await SetTime("2021-08-19-23:59:58.100", "2021-08-19-23:59:");
            await Task.Delay(4000);
            files = Directory.GetFiles(path);

            Assert.Empty(files);

            fixture.TcClient.DeleteVariableHandle(hRun);
        }

        [Fact]
        public async void New_logfile_is_created_if_rolling_interval_rolls()
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".New_logfile_is_created_if_rolling_interval_rolls_run");
            fixture.TcClient.WriteAny(hRun, false);

            await SetTime("2021-08-17-20:05:01.300", "2021-08-17-20:05:");
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            await SetTime("2021-08-17-21:59:58.300", "2021-08-17-21:59:");
            await Task.Delay(3000);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(3000);
            var files = Directory.GetFiles(path);

            Assert.True(files.Length == 2);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
        }

        [Fact]
        public async void Same_log_file_is_used_until_rolling_interval_rolls()
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Same_log_file_is_used_until_rolling_interval_rolls");
            fixture.TcClient.WriteAny(hRun, false);

            await SetTime("2021-08-17-22:05:01.300", "2021-08-17-22:05:");
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            fixture.TcClient.WriteAny(hRun, false);
            await Task.Delay(1000);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);

            Assert.True(files.Length == 1);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
        }
    }
}
