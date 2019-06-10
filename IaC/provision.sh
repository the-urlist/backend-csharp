# This processes all the command line arguments and sets them to the appropriate variable so
# all variables will be ready to be used in the rest of the script
#
echo "Number of parameters passed in: $#"
echo "Iterating through parameters..."
foundResourceGroupName=false
foundSubscriptionName=false
foundResourceGroupRegion=false
foundStorageAccountName=false
foundStorageAccountRegion=false
foundStorageAccountSku=false
foundErrorDocumentName=false
foundIndexDocumentName=false
for var in "$@"
do
    echo "    param: $var"

    # set variable values
    if [ $foundResourceGroupName = true ] ;
    then 
        echo "        setting value for resourceGroupName: $var"
        resourceGroupName=$var
        foundResourceGroupName=false
    elif [ $foundSubscriptionName = true ] ;
    then
        echo "        setting value for subscriptionName: $var"
        subscriptionName=$var
        foundSubscriptionName=false
    elif [ $foundResourceGroupRegion = true ];
    then
        echo "        setting value for resourceGroupRegion: $var"
        resourceGroupRegion=$var
        foundResourceGroupRegion=false
    elif [ $foundStorageAccountName = true ];
    then
        echo "        setting value for storageAccountName: $var"
        storageAccountName=$var
        foundStorageAccountName=false
    elif [ $foundStorageAccountRegion = true ];
    then
        echo "        setting value for storageAccountRegion: $var"
        storageAccountRegion=$var
        foundStorageAccountRegion=false
    elif [ $foundStorageAccountSku = true ];
    then
        echo "        setting value for storageAccountSku: $var"
        storageAccountSku=$var
        foundStorageAccountSku=false
    elif [ $foundErrorDocumentName = true ];
    then
        echo "        setting value for errorDocumentName: $var"
        errorDocumentName=$var
        foundErrorDocumentName=false
    elif [ $foundIndexDocumentName = true ];
    then
        echo "        setting value for indexDocumentName: $var"
        indexDocumentName=$var
        foundIndexDocumentName=false
    fi


    # get variable names
    if [ "$var" = "-resourceGroupName" ]; 
    then
        echo "        found parameter resourceGroupName"
        foundResourceGroupName=true;
    elif [ "$var" = "-subscriptionName" ];
    then
        echo "        found parameter subscriptionName"
        foundSubscriptionName=true;
    elif [ "$var" = "-resourceGroupRegion" ];
    then
        echo "        found parameter resourceGroupRegion"
        foundResourceGroupRegion=true;
    elif [ "$var" = "-storageAccountName" ];
    then
        echo "        found parameter storageAccountName"
        foundStorageAccountName=true;
    elif [ "$var" = "-storageAccountRegion" ];
    then
        echo "        found parameter storageAccountRegion"
        foundStorageAccountRegion=true;
    elif [ "$var" = "-storageAccountSku" ];
    then
        echo "        found parameter storageAccountSku"
        foundStorageAccountSku=true;
    elif [ "$var" = "-errorDocumentName" ];
    then
        echo "        found parameter errorDocumentName"
        foundErrorDocumentName=true;
    elif [ "$var" = "-indexDocumentName" ];
    then
        echo "        found parameter indexDocumentName"
        foundIndexDocumentName=true;
    fi
done
echo

# This Sets the subscription identified to be default subscription 
#
echo "Setting default subscription for Azure CLI: $subscriptionName"
az account set --subscription $subscriptionName
echo

# This creates the resource group used to house all of the URList application
#
echo "Creating resource group $resourceGroupName in region $resourceGroupRegion"
az group create --name $resourceGroupName --location $resourceGroupRegion
echo

# This creates a storage account to host our static web site
#
echo "Creating storage account $storageAccountName in resource group $resourceGroupName"
az storage account create --location $storageAccountRegion --name $storageAccountName --resource-group $resourceGroupName --sku "$storageAccountSku" --kind StorageV2
echo

# This sets the storage account so it can host a static website
#
echo "Enabling static website hosting in storage account $storageAccountName"
az extension add --name storage-preview
az storage blob service-properties update --account-name $storageAccountName --static-website --404-document $errorDocumentName --index-document $indexDocumentName

# This sets the 