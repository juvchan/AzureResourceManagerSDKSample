using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Azure.Common.Authentication.Models;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Resources.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.OData;
using Newtonsoft.Json.Linq;
using ResourceManagementSDKDemo.ARMTemplates;

namespace ResourceManagementSDKDemo.ResourceManager
{
    internal class ArmClient
    {
        private readonly AuthenticationContext _authContext;
        private readonly ClientCredential _credential;
        private readonly string _subscriptionId;
        private readonly AzureEnvironment _azureEnvironment;
        private readonly ResourceManagementClient _resourceManagementClient;

        internal ArmClient()
        {
            var tenantId = ConfigurationManager.AppSettings["AAD.TenantId"];
            _subscriptionId = ConfigurationManager.AppSettings["AzureSubscriptionId"];
            var clientId = ConfigurationManager.AppSettings["AAD.ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["AAD.ClientSecret"];

            // Set Environment - Choose between Azure public cloud, china cloud and US govt. cloud
            _azureEnvironment = AzureEnvironment.PublicEnvironments[EnvironmentName.AzureCloud];
            var authority = $"{_azureEnvironment.Endpoints[AzureEnvironment.Endpoint.ActiveDirectory]}{tenantId}";
            _authContext = new AuthenticationContext(authority);
            _credential = new ClientCredential(clientId, clientSecret);

            var tokenCloudCredential = GetTokenCloudCredential();
            var tokenCredential = new TokenCredentials(tokenCloudCredential.Token);
            _resourceManagementClient = new ResourceManagementClient(_azureEnvironment.GetEndpointAsUri(AzureEnvironment.Endpoint.ResourceManager), tokenCredential)
            {
                SubscriptionId = _subscriptionId
            };
        }

        // TODO
        // Add tags to individual resource
        // ARM deployment via template links, template

        internal async Task<IPage<ResourceGroup>> ListResourceGroupsAsync()
        {
            try
            {
                var rgListResult = await _resourceManagementClient.ResourceGroups.ListAsync().ConfigureAwait(false);
                return rgListResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        internal async Task<IPage<GenericResource>> ListWebAppsAsync()
        {
            try
            {
                var resourceTypeFilter = new ODataQuery<GenericResourceFilter>(f => f.ResourceType == "Microsoft.Web/sites" );
                var resourceListResult = await _resourceManagementClient.Resources.ListAsync(resourceTypeFilter).ConfigureAwait(false);
                return resourceListResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        internal async Task<IPage<GenericResource>> ListRmVirtualMachinesAsync()
        {
            try
            {
                var resourceTypeFilter = new ODataQuery<GenericResourceFilter>(f => f.ResourceType == "Microsoft.Compute/virtualMachines");
                var resourceListResult = await _resourceManagementClient.Resources.ListAsync(resourceTypeFilter).ConfigureAwait(false);
                return resourceListResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        // https://msdn.microsoft.com/en-us/library/azure/dn790529.aspx
        // $filter can be used to restrict the results to specific tagged resources. The following possible values can be used with $filter:
        // $filter = tagname eq { value}
        // $filter = tagname eq { tagname} and tagvalue eq { tagvalue}
        // $filter = startswith(tagname, { tagname prefix})
        internal async Task<IPage<ResourceGroup>> ListResourceGroupsAsync(string tagName)
        {
            try
            {
                var rgFilter = new ODataQuery<ResourceGroupFilter>();
                rgFilter.SetFilter(f => f.TagName == tagName );
        
                var rgListResult = await _resourceManagementClient.ResourceGroups.ListAsync(rgFilter).ConfigureAwait(false);
                return rgListResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        internal async Task<ResourceGroup> CreateOrUpdateResourceGroupAsync(string rgName, string rgLocation)
        {
            try
            {
                var rgParam = new ResourceGroup() { Name = rgName, Location = rgLocation };
                return await _resourceManagementClient.ResourceGroups.CreateOrUpdateAsync(rgName, rgParam).ConfigureAwait(false);
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        internal async Task<ResourceGroup> CreateOrUpdateResourceGroupWithTagAsync(string rgName, string rgLocation, string tagName, string tagValue)
        {
            try
            {
                var tag = new Dictionary<string, string> {{tagName, tagValue}};

                var rgParam = new ResourceGroup() { Name = rgName, Location = rgLocation, Tags = tag };
                return await _resourceManagementClient.ResourceGroups.CreateOrUpdateAsync(rgName, rgParam).ConfigureAwait(false);
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }


        internal async Task DeleteResourceGroup(string rgName)
        {
            try
            {
                await _resourceManagementClient.ResourceGroups.DeleteAsync(rgName).ConfigureAwait(false);
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        // https://azure.microsoft.com/en-gb/documentation/articles/resource-group-authoring-templates/
        // Using Azure QuickStart Templates - Web-App-Sql-Database Sample Template from Github 
        // https://github.com/Azure/azure-quickstart-templates/tree/master/201-web-app-sql-database
        internal async Task<DeploymentExtended> ArmTemplateDeploymentFromLinks(string rgName, string rgLocation)
        {
            try
            {
                var isRgExist = await IsResourceGroupExistsAsync(rgName).ConfigureAwait(false);

                var deploymentName = $"{rgName}-deployment";
                var templateJsonLink = new Uri("https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/201-web-app-sql-database/azuredeploy.json");
                var parametersJsonLink = new Uri("https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/201-web-app-sql-database/azuredeploy.parameters.json");

                if (!isRgExist.HasValue || isRgExist.Value == false)
                {
                    await CreateOrUpdateResourceGroupWithTagAsync(rgName, rgLocation, nameof(deploymentName), deploymentName ).ConfigureAwait(false);
                }

                var deploymentProperties = new DeploymentProperties()
                {
                    Mode = DeploymentMode.Incremental,
                    ParametersLink = new ParametersLink(parametersJsonLink.AbsoluteUri),
                    TemplateLink = new TemplateLink(templateJsonLink.AbsoluteUri)
                };

                var deploymentParams = new Deployment() { Properties = deploymentProperties};
                var deploymentResult = await _resourceManagementClient.Deployments.CreateOrUpdateAsync(rgName, deploymentName, deploymentParams).ConfigureAwait(false);
                return deploymentResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        /// <summary>
        /// ARM Template deployment using embedded Template Json file and Parameter Json file
        /// Deploy A single instance Free Tier Azure Web App
        /// </summary>
        /// <param name="rgName"></param>
        /// <param name="rgLocation"></param>
        /// <param name="webAppName"></param>
        /// <returns></returns>
        internal async Task<DeploymentExtended> ArmTemplateDeploymentFromFiles(string rgName, string rgLocation, string webAppName)
        {
            try
            {
                var isRgExist = await IsResourceGroupExistsAsync(rgName).ConfigureAwait(false);

                var deploymentName = $"{rgName}-deployment";

                if (!isRgExist.HasValue || isRgExist.Value == false)
                {
                    await CreateOrUpdateResourceGroupWithTagAsync(rgName, rgLocation, nameof(deploymentName), deploymentName).ConfigureAwait(false);
                }

                var templateJson = ArmTemplateResources.SampleTemplate.Trim();
                var paramJsonRaw = ArmTemplateResources.SampleTemplate_param.Trim();

                // Update param Json values
                dynamic paramJson = JObject.Parse(paramJsonRaw);
                paramJson.appServicePlanName.value.Value = $"{webAppName}-plan";
                paramJson.webAppName.value.Value = webAppName;

                var deploymentProperties = new DeploymentProperties()
                {
                    Mode = DeploymentMode.Incremental,
                    Parameters = paramJson.ToString(),
                    Template = templateJson
                };

                var deploymentParams = new Deployment() { Properties = deploymentProperties };
                //var deploymentValidationResult = await _resourceManagementClient.Deployments.ValidateAsync(rgName, deploymentName, deploymentParams).ConfigureAwait(false);    
                var deploymentResult = await _resourceManagementClient.Deployments.CreateOrUpdateAsync(rgName, deploymentName, deploymentParams).ConfigureAwait(false);
                return deploymentResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Response.ReasonPhrase}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        internal async Task<DeploymentExtended> ArmTemplateDeploymentFromTemplateLinkAndParamFile(string rgName, string rgLocation)
        {
            try
            {
                var isRgExist = await IsResourceGroupExistsAsync(rgName).ConfigureAwait(false);

                var deploymentName = $"{rgName}-deployment";

                if (!isRgExist.HasValue || isRgExist.Value == false)
                {
                    await CreateOrUpdateResourceGroupWithTagAsync(rgName, rgLocation, nameof(deploymentName), deploymentName).ConfigureAwait(false);
                }

                var paramJsonRaw = ArmTemplateResources._201_web_app_sql_database_param;

                // Update param Json values
                // Using JSON.NET for dynamic JSON parsing
                // http://weblog.west-wind.com/posts/2012/Aug/30/Using-JSONNET-for-dynamic-JSON-parsing
                var paramJson = JObject.Parse(paramJsonRaw) as dynamic;
                paramJson.hostingPlanName.value.Value = $"{rgName}-plan";
                paramJson.siteName.value.Value = $"{rgName}-web";
                paramJson.siteLocation.value.Value = rgLocation;
                paramJson.serverName.value.Value = $"{rgName}-sqlserver";
                paramJson.serverLocation.value.Value = rgLocation;
                paramJson.administratorLogin.value.Value = "juvchan";
                paramJson.administratorLoginPassword.value.Value = "aj2A@$31";
                paramJson.databaseName.value.Value = $"{rgName}-sqldb";

                var deploymentProperties = new DeploymentProperties
                {
                    Mode = DeploymentMode.Incremental,
                    Parameters = paramJson.ToString(),
                    TemplateLink = new TemplateLink
                    {
                        Uri = "https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/201-web-app-sql-database/azuredeploy.json"
                    }
                };

                var deploymentParams = new Deployment { Properties = deploymentProperties };
                var deploymentResult = await _resourceManagementClient.Deployments.CreateOrUpdateAsync(rgName, deploymentName, deploymentParams).ConfigureAwait(false);
                return deploymentResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Response.ReasonPhrase}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        private async Task<bool?> IsResourceGroupExistsAsync(string rgName)
        {
            try
            {
                var isExistResult =
                    await _resourceManagementClient.ResourceGroups.CheckExistenceAsync(rgName).ConfigureAwait(false);
                return isExistResult;
            }
            catch (CloudException cex)
            {
                var error = $"{cex.GetType().FullName}: {cex.Body.Code} : {cex.Body.Message}";
                throw new CloudException(error);
            }
        }

        private TokenCloudCredentials GetTokenCloudCredential()
        {
            try
            {
                var authResult = _authContext.AcquireToken(_azureEnvironment.Endpoints[AzureEnvironment.Endpoint.ActiveDirectoryServiceEndpointResourceId], _credential);
                return new TokenCloudCredentials(_subscriptionId, authResult.AccessToken);
            }
            catch (AdalException adalex)
            {
                var error = $"{adalex.GetType().FullName}: {adalex.Message}";
                throw new AdalException(adalex.ErrorCode, error);
            }
        }
    }
}