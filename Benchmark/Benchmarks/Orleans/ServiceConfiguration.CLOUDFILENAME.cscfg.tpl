<?xml version="1.0" encoding="utf-8"?>
<ServiceConfiguration serviceName="Orleans" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration" osFamily="4" osVersion="*" schemaVersion="2015-04.2.6">
  <Role name="Orleans.Frontend">
    <Instances count="4" />
    <ConfigurationSettings>
      <Setting name="Conductor" value="CONDUCTORDOMAINCLOUDAPP.cloudapp.net" />
      <Setting name="MultiCluster" value="uswest" />
      <Setting name="DataCenter" value="DOMAINCLOUDAPP.cloudapp.net" />
      <Setting name="BenchmarkStorage" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="DataConnectionString" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="APPINSIGHTS_INSTRUMENTATIONKEY" value="35275a0d-f371-4b03-89f7-765d917bcff2" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" value="true" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" value="REMOTEDESKTOPUSER" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" value="REMOTEDESKTOPPASS" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" value="2019-08-24T23:59:59.0000000-07:00" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled" value="true" />
    </ConfigurationSettings>
    <Certificates>
      <Certificate name="Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" thumbprint="E3D564F5ED98A62F66CB7C9B4842B3DD3A3D8CAE" thumbprintAlgorithm="sha1" />
    </Certificates>
  </Role>
  <Role name="Orleans.Silos">
    <Instances count="2" />
    <ConfigurationSettings>
      <Setting name="DataCenter" value="DOMAINCLOUDAPP.cloudapp.net" />
      <Setting name="BenchmarkStorage" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="DataConnectionString" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="DefaultEndpointsProtocol=https;AccountName=MAINSTORAGEACCOUNT;AccountKey=MAINSTORAGEACCOUNTKEY" />
      <Setting name="APPINSIGHTS_INSTRUMENTATIONKEY" value="ad39a5a0-74ad-4b34-8361-a7397e42a09d" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled" value="true" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername" value="REMOTEDESKTOPUSER" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword" value="REMOTEDESKTOPPASS" />
      <Setting name="Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration" value="2019-08-24T23:59:59.0000000-07:00" />
    </ConfigurationSettings>
    <Certificates>
      <Certificate name="Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption" thumbprint="WINDOWSAZUREPLUGINTUMBPRINT" thumbprintAlgorithm="sha1" />
    </Certificates>
  </Role>
  <!--<NetworkConfiguration>
    <VirtualNetworkSite name="VNet1" />
  </NetworkConfiguration>-->
</ServiceConfiguration>