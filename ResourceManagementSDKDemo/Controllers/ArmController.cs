using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Management.Resources.Models;
using Microsoft.Rest.Azure;
using ResourceManagementSDKDemo.ResourceManager;

namespace ResourceManagementSDKDemo.Controllers
{
    [RoutePrefix("api/arm")]
    public class ArmController : ApiController
    {
        private readonly ArmClient _armClient;

        public ArmController()
        {
            _armClient = new ArmClient();
        }

        [HttpGet]
        [Route("ResourceGroups")]
        public async Task<IPage<ResourceGroup>> GetResourceGroupsAsync()
        {
            return await _armClient.ListResourceGroupsAsync();
        }


        [HttpGet]
        [Route("ResourceGroups/tag/{tagName}")]
        public async Task<IPage<ResourceGroup>> GetResourceGroupsAsync([FromUri] string tagName)
        {
            return await _armClient.ListResourceGroupsAsync(tagName);
        }

        [HttpGet]
        [Route("Resources/type/WebApp")]
        public async Task<IPage<GenericResource>> GetWebAppResourcesAsync()
        {
            return await _armClient.ListWebAppsAsync();
        }

        [HttpGet]
        [Route("Resources/type/VM")]
        public async Task<IPage<GenericResource>> GetVmResourcesAsync()
        {
            return await _armClient.ListRmVirtualMachinesAsync();
        }

        [HttpPut]
        [Route("ResourceGroup/{resourceGroupName}/{resourceGroupLocation}/tag/{tagName}/{tagValue}")]
        public async Task<ResourceGroup> CreateOrUpdateResourceGroupWithTagAsync([FromUri] string resourceGroupName, [FromUri] string resourceGroupLocation, [FromUri] string tagName, [FromUri] string tagValue)
        {
            return await _armClient.CreateOrUpdateResourceGroupWithTagAsync(resourceGroupName, resourceGroupLocation, tagName, tagValue);
        }

        [HttpPut]
        [Route("ResourceGroup/{resourceGroupName}/{resourceGroupLocation}")]
        public async Task<ResourceGroup> CreateOrUpdateResourceGroupAsync([FromUri] string resourceGroupName, [FromUri] string resourceGroupLocation)
        {
            return await _armClient.CreateOrUpdateResourceGroupAsync(resourceGroupName, resourceGroupLocation);
        }

        [HttpPut]
        [Route("ResourceGroup/{resourceGroupName}/{resourceGroupLocation}/deployment")]
        public async Task<DeploymentExtended> ArmTemplateDeploymentFromLinksAsync([FromUri] string resourceGroupName, [FromUri] string resourceGroupLocation)
        {
            return await _armClient.ArmTemplateDeploymentFromLinks(resourceGroupName, resourceGroupLocation);
        }

        [HttpPut]
        [Route("ResourceGroup/{resourceGroupName}/{resourceGroupLocation}/{webAppName}/deployment")]
        public async Task<DeploymentExtended> ArmTemplateDeploymentFromFilesAsync([FromUri] string resourceGroupName, [FromUri] string resourceGroupLocation, [FromUri] string webAppName)
        {
            return await _armClient.ArmTemplateDeploymentFromFiles(resourceGroupName, resourceGroupLocation, webAppName);
        }

        [HttpPut]
        [Route("ResourceGroup/{resourceGroupName}/{resourceGroupLocation}/deployment/templateLink/paramFile")]
        public async Task<DeploymentExtended> ArmTemplateDeploymentFromTemplateLinkAndParamFileAsync([FromUri] string resourceGroupName, [FromUri] string resourceGroupLocation)
        {
            return await _armClient.ArmTemplateDeploymentFromTemplateLinkAndParamFile(resourceGroupName, resourceGroupLocation);
        }

        [HttpDelete]
        [Route("ResourceGroup/{resourceGroupName}")]
        public async Task DeleteResourceGroupAsync([FromUri] string resourceGroupName)
        {
            await _armClient.DeleteResourceGroup(resourceGroupName);
        }
    }
}
