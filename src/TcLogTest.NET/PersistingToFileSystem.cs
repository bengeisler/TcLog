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
        readonly string mut = "TESTS.TestWrapper";
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
            
            hPath = fixture.TcClient.CreateVariableHandle(mut + ".FilePath");
            hFileName = fixture.TcClient.CreateVariableHandle(mut + ".FileName");
            fixture.TcClient.WriteAny(hPath, path);
            fixture.TcClient.WriteAny(hFileName, filename);

            Directory.CreateDirectory(path);
            var files = Directory.GetFiles(path);
            foreach (var f in files) File.Delete(f);

            hSystemTime = fixture.TcClient.CreateVariableHandle(mut + ".NewLocalSystemTimeToBeSet");
            hSetSystemTime = fixture.TcClient.CreateVariableHandle(mut + ".TriggerNewLocalSystemTime");
            hTime = fixture.TcClient.CreateVariableHandle(mut + ".LocalTimeAsString");
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
            string message = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata";
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Persist_long_error_message_run");
            uint hData = fixture.TcClient.CreateVariableHandle(mut + ".Persist_long_error_message_data");

            fixture.TcClient.WriteAny(hData, message);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllText(files[0]);

            Assert.Equal($"{message[..254]}\r\n", fileContent);

            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hData);
        }

        [Fact]
        public async void Linebreak_is_included_when_log_message_has_maximum_length()
        {
            string message = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata";
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Linebreak_is_included_when_log_message_has_maximum_length_run");
            uint hData = fixture.TcClient.CreateVariableHandle(mut + ".Linebreak_is_included_when_log_message_has_maximum_length_data");

            fixture.TcClient.WriteAny(hData, message);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(1000);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllText(files[0]);

            Assert.Equal($"{message[..254]}\r\n{message[..254]}\r\n", fileContent);

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

            delimiter = "";
            fixture.TcClient.WriteAny(hDelimiter, delimiter);
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

        [Theory]
        [InlineData(1, 1000)]
        [InlineData(5, 1000)]
        [InlineData(10, 1000)]
        [InlineData(20, 2000)]
        [InlineData(100, 5000)]
        public async void Log_in_consecutive_cycles(int cycleCount, int waitTime)
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Log_in_consecutive_cycles");
            uint hCycles = fixture.TcClient.CreateVariableHandle(mut + ".Number_of_log_cycles");

            fixture.TcClient.WriteAny(hCycles, cycleCount);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(waitTime);
            var files = Directory.GetFiles(path);

            var fileContent = File.ReadAllLines(files[0]);

            Assert.Equal<int>(cycleCount, fileContent.Length);

            fixture.TcClient.WriteAny(hCycles, 0);
            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hCycles);
        }

        [Theory]
        [InlineData(1, 1000)]
        [InlineData(5, 1000)]
        [InlineData(10, 1000)]
        [InlineData(20, 2000)]
        [InlineData(100, 5000)]
        public async void Log_multiple_times_in_one_cycle(int logCount, int waitTime)
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Log_multiple_logs_in_one_cycle");
            uint hLogCount = fixture.TcClient.CreateVariableHandle(mut + ".Number_of_logs_per_cycle");

            fixture.TcClient.WriteAny(hLogCount, logCount);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(waitTime);
            var files = Directory.GetFiles(path);

            var fileContent = File.ReadAllLines(files[0]);

            Assert.Equal<int>(logCount, fileContent.Length);

            fixture.TcClient.WriteAny(hLogCount, 0);
            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hLogCount);
        }

        [Theory]
        [InlineData(5, 5, 500)]
        [InlineData(10, 10, 1_000)]
        [InlineData(20, 20, 2_000)]
        [InlineData(50, 50, 5_000)]
        [InlineData(5, 200, 2_000)]
        [InlineData(5, 500, 5_000)]
        [InlineData(20, 200, 10_000)]
        public async void Log_multiple_times_in_multiple_cycles(int cycleCount, int logCount, int waitTime)
        {
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Log_multiple_logs_in_multiple_cycles");
            uint hCyclesToLog = fixture.TcClient.CreateVariableHandle(mut + ".Number_of_cycles");
            uint hLogCount = fixture.TcClient.CreateVariableHandle(mut + ".Number_of_logs_per_cycle");

            fixture.TcClient.WriteAny(hCyclesToLog, cycleCount);
            fixture.TcClient.WriteAny(hLogCount, logCount);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(waitTime);
            var files = Directory.GetFiles(path);
            var fileContent = File.ReadAllLines(files[0]);

            Assert.Equal<int>(cycleCount * logCount, fileContent.Length);

            fixture.TcClient.WriteAny(hCyclesToLog, 0);
            fixture.TcClient.WriteAny(hLogCount, 0);
            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hCyclesToLog);
            fixture.TcClient.DeleteVariableHandle(hLogCount);
        }

        [Theory]
        [InlineData(1, 1, 1_000)]
        [InlineData(5, 5, 1_000)]
        [InlineData(10, 1, 1_000)]
        [InlineData(1, 10, 1_000)]
        [InlineData(10, 10, 5_000)]
        [InlineData(20, 20, 5_000)]
        [InlineData(50, 50, 5_000)]
        [InlineData(100, 50, 10_000)]
        [InlineData(5, 200, 2_000)]
        [InlineData(5, 500, 5_000)]
        public async void Persistance_time_stays_within_bounds(int cycleCount, int logCount, int waitTime)
        {
            const int MAX_LOGS_PER_CYCLE = 100;
            uint hRun = fixture.TcClient.CreateVariableHandle(mut + ".Persistance_time_stays_within_bounds");
            uint hCyclesToLog = fixture.TcClient.CreateVariableHandle(mut + ".Number_of_cycles");
            uint hLogCount = fixture.TcClient.CreateVariableHandle(mut + ".Number_of_logs_per_cycle");
            uint hDurationInCycles = fixture.TcClient.CreateVariableHandle(mut + ".Duration_in_cylces");
            int expectedCycles =
                logCount > MAX_LOGS_PER_CYCLE ?
                logCount / MAX_LOGS_PER_CYCLE * 3 * cycleCount :
                cycleCount;
           
            fixture.TcClient.WriteAny(hCyclesToLog, cycleCount);
            fixture.TcClient.WriteAny(hLogCount, logCount);
            fixture.TcClient.WriteAny(hDurationInCycles, 0);
            fixture.TcClient.WriteAny(hRun, true);
            await Task.Delay(waitTime);
            var files = Directory.GetFiles(path);
            var durationInCycles = fixture.TcClient.ReadAny<int>(hDurationInCycles);

            Assert.InRange(durationInCycles, expectedCycles, expectedCycles * 1.5 + 2);

            fixture.TcClient.WriteAny(hCyclesToLog, 0);
            fixture.TcClient.WriteAny(hLogCount, 0);
            fixture.TcClient.WriteAny(hDurationInCycles, 0);
            foreach (var f in files) File.Delete(f);
            fixture.TcClient.DeleteVariableHandle(hRun);
            fixture.TcClient.DeleteVariableHandle(hCyclesToLog);
            fixture.TcClient.DeleteVariableHandle(hLogCount);
            fixture.TcClient.DeleteVariableHandle(hDurationInCycles);
        }

        [Fact]
        public void Persistence_in_first_cpu_cycle_has_correct_time_data_in_filename()
        {
            var files = Directory.GetFiles("C:\\UnitTestFirstCycle\\");
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var day = DateTime.Now.Day;

            foreach (var f in files) { 
                Assert.DoesNotContain("1970", f);
                Assert.Contains(year.ToString(), f);
                Assert.Contains(month.ToString(), f);
                Assert.Contains(day.ToString(), f);
            }

            foreach (var f in files) File.Delete(f);
        }
    }
}
