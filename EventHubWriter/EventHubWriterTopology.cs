using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System.Configuration;

namespace EventHubWriter
{
    /// <summary>
    /// A hybrid C#/Java topology
    ///     The C# spout creates random 'device' data
    ///     Data is then written to Event Hub using the Java bolt
    /// </summary>
    [Active(true)]
    class EventHubWriter : TopologyDescriptor
    {
        static void Main(string[] args)
        {
        }
        /// <summary>
        /// Builds a topology that can be submitted to Storm on HDInsight
        /// </summary>
        /// <returns>A topology builder</returns>
        public ITopologyBuilder GetTopologyBuilder()
        {
            //The friendly name is 'EventHubWriter'
            TopologyBuilder topologyBuilder = new TopologyBuilder("EventHubWriter" + DateTime.Now.ToString("yyyyMMddHHmmss"));

            //Get the partition count
            int partitionCount = int.Parse(ConfigurationManager.AppSettings["EventHubPartitionCount"]);
            //Create a deserializer for JSON to java.lang.String
            //so that Java components can consume data emitted by
            //C# components
            List<string> javaDeserializerInfo =
                new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer", "java.lang.String" };

            //Set the spout
            topologyBuilder.SetSpout(
                "Spout",
                Spout.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"Event"}}
                },
                partitionCount). //Parallelism hint uses partition count
                DeclareCustomizedJavaDeserializer(javaDeserializerInfo); //Deserializer for the output stream

            //Create constructor for the Java bolt
            JavaComponentConstructor constructor =
                JavaComponentConstructor.CreateFromClojureExpr(
                    String.Format(@"(org.apache.storm.eventhubs.bolt.EventHubBolt. (org.apache.storm.eventhubs.bolt.EventHubBoltConfig. " +
                    @"""{0}"" ""{1}"" ""{2}"" ""{3}"" ""{4}"" {5}))",
                ConfigurationManager.AppSettings["EventHubPolicyName"],
                ConfigurationManager.AppSettings["EventHubPolicyKey"],
                ConfigurationManager.AppSettings["EventHubNamespace"],
                "servicebus.windows.net", //suffix for servicebus fqdn
                ConfigurationManager.AppSettings["EventHubName"],
                "true"));

            topologyBuilder.SetJavaBolt(
                    "EventHubBolt",
                    constructor,
                    partitionCount). //Parallelism hint uses partition count
                shuffleGrouping("Spout"); //Consume data from spout

            StormConfig config = new StormConfig();
            config.setNumWorkers(1); //Set the number of workers
            topologyBuilder.SetTopologyConfig(config);

            return topologyBuilder;
        }
    }
}

