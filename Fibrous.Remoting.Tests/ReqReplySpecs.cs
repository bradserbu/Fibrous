﻿namespace Fibrous.Remoting.Tests
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    
    using FluentAssertions;
    using NUnit.Framework;
    using NetMQ;

    [TestFixture]
    public class WhenRequestIsSent2 : ReqReplyServiceSpecs
    {
        [Test]
        public void Test()
        {
            Client.SendRequest("test",
                ClientFiber,
                x =>
                {
                    Reply = x;
                    Replied.Set();
                });
            Replied.WaitOne(TimeSpan.FromSeconds(1));
            Cleanup();
            Reply.Should().BeEquivalentTo("TEST");
        }
    }

    [TestFixture]
    public class WhenMultipleRequestsAreSent2 : ReqReplyServiceSpecs
    {
        [Test]
        public void Test()
        {
            for (int i = 0; i < 100; i++)
            {
                Client.SendRequest("test" + i,
                    ClientFiber,
                    x =>
                    {
                        Reply = x;
                        if (x == "TEST99")
                            Replied.Set();
                    });
            }
            Replied.WaitOne(TimeSpan.FromSeconds(10));
            Cleanup();
            Reply.Should().BeEquivalentTo("TEST99");
        }
    }

    [TestFixture]
    public class CanSendALotFast2 : ReqReplyServiceSpecs
    {
        private const int count = 100000;
        private static readonly string EndReply = "TEST" + (count - 1).ToString();

        [Test]
        public void Test()
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                Client.SendRequest("test" + i,
                    ClientFiber,
                    x =>
                    {
                        Reply = x;
                        if (x == EndReply)
                            Replied.Set();
                    });
            }
            Replied.WaitOne(TimeSpan.FromSeconds(20));
            sw.Stop();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds);
            Reply.Should().BeEquivalentTo(EndReply);
            Cleanup();
        }
    }

    public abstract class ReqReplyServiceSpecs
    {
        protected static RequestHandlerSocket<string, string> Service;
        protected static RequestSocket<string, string> Client;
        protected static IFiber ClientFiber;
        protected static IFiber ServerFiber;
        protected static string Reply;
        protected static ManualResetEvent Replied;
        protected static NetMQContext ClientContext;
        protected static NetMQContext ServerContext;

        public ReqReplyServiceSpecs()
        {
            Reply = string.Empty;
            Replied = new ManualResetEvent(false);
            Console.WriteLine("Start client fiber");
            ClientFiber = PoolFiber.StartNew();
            ClientContext = NetMQContext.Create();
            ServerFiber = PoolFiber.StartNew();
            ServerContext = NetMQContext.Create();
            Func<byte[], string> unmarshaller = x => Encoding.Unicode.GetString(x);
            Func<string, byte[]> marshaller = x => Encoding.Unicode.GetBytes(x);
            Service = new RequestHandlerSocket<string, string>(ServerContext,
                "tcp://Monster:9997",
                unmarshaller,
                marshaller);
            Service.SetRequestHandler(ServerFiber, request => request.Reply(request.Request.ToUpper()));
            Console.WriteLine("Start service");
            Client = new RequestSocket<string, string>(ClientContext,
                "tcp://Monster:9997",
                marshaller,
                unmarshaller);
            Console.WriteLine("Start client");
        }

        protected void Cleanup()
        {
            ClientFiber.Enqueue(() =>
            {
                Client.Dispose();
                ClientContext.Dispose();
                Thread.Sleep(10);
                Service.Dispose();
                ServerContext.Dispose();
            });
            Thread.Sleep(100);
            Console.WriteLine("Dispose client fiber");
            ClientFiber.Dispose();
            Console.WriteLine("Dispose service");
            ServerFiber.Dispose();
        }
    }
}