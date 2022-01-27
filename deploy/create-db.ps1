$location="West Europe";
$resourceGroup="tpapp-gf-training-sql";
$server="sqlsrv-euw-tpapp-gf-training";
$database="sqldb-euw-tpapp-gf-training";
$login="SqlAdminUser";
$password="put password here";
$myIp="put your ip here";

az group create --name $resourceGroup --location "$location"
az sql server create --name $server --resource-group $resourceGroup --location "$location" --admin-user $login --admin-password $password
az sql db create --resource-group $resourceGroup --server $server --name $database --edition GeneralPurpose --family Gen5 --capacity 2 --zone-redundant --compute-model Serverless --auto-pause-delay 60 --min-capacity 0.5 --sample-name AdventureWorksLT
az sql server firewall-rule create --resource-group $resourceGroup --server $server -n AllowYourIp --start-ip-address $myIp --end-ip-address $myIp

az group delete --name $resourceGroup

# az deployment group create --name ExampleDeployment --resource-group $resourceGroup --template-file azuredeploy-sql.json --parameters adminLogin=$login adminPassword=$password serverName=$server databaseName=$database
