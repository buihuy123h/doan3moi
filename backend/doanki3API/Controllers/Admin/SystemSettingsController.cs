using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using doanki3API.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanki3API.Data;
using doanki3API.Models;

namespace doanki3API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = EmployeeRoles.Manager)]
    public class SystemSettingsController : ControllerBase
    {
        private readonly MyAppDbContext _context;

        public SystemSettingsController(MyAppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SystemSetting>>> GetSettings()
        {
            return await _context.SystemSettings.AsNoTracking().ToListAsync();
        }

        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] SystemSetting settingUpdate)
        {
            var setting = await _context.SystemSettings.FindAsync(key);
            if (setting == null)
            {
                setting = new SystemSetting 
                { 
                    SettingKey = key,
                    SettingValue = settingUpdate.SettingValue,
                    Category = settingUpdate.Category ?? "General",
                    DataType = settingUpdate.DataType ?? "string",
                    Description = settingUpdate.Description,
                    LastUpdated = DateTime.Now
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = settingUpdate.SettingValue;
                setting.LastUpdated = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(setting);
        }
    }
}
