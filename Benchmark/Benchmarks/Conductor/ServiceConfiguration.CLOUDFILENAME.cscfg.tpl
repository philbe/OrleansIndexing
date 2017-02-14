<?xml version="1.0" encoding="utf-8"?>
<ServiceConfiguration serviceName="Conductor" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration" osFamily="4" osVersion="*" schemaVersion="2015-04.2.6">
  <Role name="Conductor.WebRole">
    <Instances count="1" />
    <ConfigurationSettings>
      <Setting name="BenchmarkStorage" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="DataConnectionString" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="APPINSIGHTS_INSTRUMENTATIONKEY" value="f3b41788-3d17-4c3f-a02c-c7476459012c" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" value="true" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" value="REMOTEDESKTOPUSERCONDUCTOR" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" value="REMOTEDESKTOPPASSCONDUCTOR" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" value="2019-07-22T23:59:59.0000000-07:00" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" value="true" />
    </ConfigurationSettings>
    <Certificates>
      <Certificate name="Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" thumbprint="WINDOWSAZUREPLUGINTUMBPRINTCONDUCTOR" thumbprintAlgorithm="sha1" />
    </Certificates>
  </Role>
</ServiceConfiguration>