using KianaBH.Data.Models.Sdk;
using Microsoft.AspNetCore.Mvc;

namespace KianaBH.SdkServer.Handlers.Sdk;

[ApiController]
public class DeviceFingerprintController : ControllerBase
{
    [HttpPost("/device-fp/api/getFp")]
    public IActionResult GetDeviceFingerprint([FromBody] GetDeviceFingerprintRequest request)
    {
        return Ok(new ResponseBase
        {
            Data = new { request.DeviceFp, Code = 0, Msg = "ok" }
        });
    }

    [HttpGet("/device-fp/api/getExtList")]
    public IActionResult GetExtList()
    {
        var extList = new[]
        {
            "cpuName",
            "deviceModel",
            "deviceName",
            "deviceType",
            "deviceUID",
            "gpuID",
            "gpuName",
            "gpuAPI",
            "gpuVendor",
            "gpuVersion",
            "gpuMemory",
            "osVersion",
            "cpuCores",
            "cpuFrequency",
            "gpuVendorID",
            "isGpuMultiTread",
            "memorySize",
            "screenSize",
            "engineName",
            "addressMAC",
            "packageVersion"
        };

        return Ok(new ResponseBase
        {
            Data = new
            {
                code = 200,
                msg = "ok",
                ext_list = extList,
                pkg_list = Array.Empty<object>()
            }
        });
    }
}