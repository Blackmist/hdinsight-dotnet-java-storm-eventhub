﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System.Configuration;

/// <summary>
/// This program shows the ability to create a SCP.NET topology consuming JAVA Spouts
/// For how to use SCP.NET, please refer to: http://go.microsoft.com/fwlink/?LinkID=525500&clcid=0x409
/// For more Storm samples, please refer to our GitHub repository: http://go.microsoft.com/fwlink/?LinkID=525495&clcid=0x409
/// </summary>

namespace EventHubReader
{
    /// <summary>
    /// TopologyBuilder hybrid topology example with Java Spout and CSharp Bolt
    /// This TopologyDescriptor is marked as Active
    /// </summary>
    [Active(true)]
    public class EventHubReader : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            // Start building a new topology
            TopologyBuilder topologyBuilder = new TopologyBuilder(typeof(EventHubReader).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));
            // Get the number of partitions in EventHub
            var eventHubPartitions = int.Parse(ConfigurationManager.AppSettings["EventHubPartitions"]);
            // Add the EvetnHubSpout to the topology. Set parallelism hint to the number of partitions
            topologyBuilder.SetEventHubSpout(
                "com.microsoft.eventhubs.spout.EventHubSpout",
                new EventHubSpoutConfig(
                    ConfigurationManager.AppSettings["EventHubSharedAccessKeyName"],
                    ConfigurationManager.AppSettings["EventHubSharedAccessKey"],
                    ConfigurationManager.AppSettings["EventHubNamespace"],
                    ConfigurationManager.AppSettings["EventHubEntityPath"],
                    eventHubPartitions),
                eventHubPartitions);

            // Set a customized JSON Serializer to serialize a Java object (emitted by Java Spout) into JSON string
            // Here, full name of the Java JSON Serializer class is required
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            // Create a config for the bolt. It's unused here
            var boltConfig = new StormConfig();

            // Add the logbolt to the topology
            topologyBuilder.SetBolt(
                typeof(LogBolt).Name,
                LogBolt.Get,
                new Dictionary<string, List<string>>(),
                eventHubPartitions,
                true
                ).
                DeclareCustomizedJavaSerializer(javaSerializerInfo).
                shuffleGrouping("com.microsoft.eventhubs.spout.EventHubSpout");
            // Create a configuration for the topology
            var topologyConfig = new StormConfig();
            // Increase max pending for the spout
            topologyConfig.setMaxSpoutPending(8192);
            // Parallelism hint for the number of workers to match the number of EventHub partitions
            topologyConfig.setNumWorkers(eventHubPartitions);
            // Add the config and return the topology builder
            topologyBuilder.SetTopologyConfig(topologyConfig);
            return topologyBuilder;
        }
    }
}
