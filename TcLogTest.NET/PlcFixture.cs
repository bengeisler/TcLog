using System;
using System.Collections.Generic;
using System.Text;
using TwinCAT.Ads;
using Xunit;

namespace TcLogTest.NET
{
    public class PlcFixture : IDisposable
    {
        public PlcFixture()
        {
            TcClient = new AdsClient();
            TcClient.Connect(851);
        }

        public void Dispose()
        {
            TcClient.Dispose();
        }

        public AdsClient TcClient { get; private set; }
    }

    [CollectionDefinition("Plc collection")]
    public class PlcCollection : ICollectionFixture<PlcFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
