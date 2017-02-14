<?xml version="1.0" encoding="utf-8"?>
<OrleansConfiguration xmlns="urn:orleans">
  <Globals>
    <Liveness LivenessType="AzureTable" />
    <StorageProviders>
      <Provider Type="Orleans.Storage.MemoryStorage" Name="Default" />
      <Provider Type="Orleans.Storage.MemoryStorage" Name="MemoryStore" />
      <Provider Type="Orleans.Storage.AzureBlobStorage" Name="IndexingStorageProvider" DataConnectionString="DefaultEndpointsProtocol=https;AccountName=INDEXINGSTORAGEACCOUNT;AccountKey=INDEXINGSTORAGEACCOUNTKEY" />
      <Provider Type="Orleans.Storage.AzureBlobStorage" Name="IndexingWorkflowQueueStorageProvider" DataConnectionString="DefaultEndpointsProtocol=https;AccountName=INDEXINGWORKFLOWSTORAGEACCOUNT;AccountKey=INDEXINGWORKFLOWSTORAGEACCOUNTKEY" />
      <Provider Type="Orleans.Storage.AzureTableStorage" Name="BenchmarkStore" DataConnectionString="DefaultEndpointsProtocol=https;AccountName=GRAINSTORAGEACCOUNT;AccountKey=GRAINSTORAGEACCOUNTKEY" />
      <Provider Type="Orleans.StorageProvider.DocumentDB.DocumentDBStorageProvider" Name="DocumentDBStore" Url="DOCUMENTDBURL" Key="DOCUMENTDBKEY" Database="test" OfferType="DOCUMENTDBOFFERTYPE" IndexingMode="DOCUMENTDBINDEXINGMODE" />
    </StorageProviders>
  </Globals>
  <Defaults>
    <Tracing DefaultTraceLevel="Info" TraceToConsole="false" TraceToFile="{0}-{1}.log" PropagateActivityId="false" WriteMessagingTraces="false" BulkMessageLimit="1000">
      <!--
      <TraceLevelOverride LogPrefix="Runtime.MembershipOracle" TraceLevel="Verbose" />
      -->
    </Tracing>
    <Telemetry>
      <TelemetryConsumer Type="Orleans.TelemetryConsumers.AI.AITelemetryConsumer" Assembly="OrleansTelemetryConsumers.AI"/>
    </Telemetry>
  </Defaults>
</OrleansConfiguration>
