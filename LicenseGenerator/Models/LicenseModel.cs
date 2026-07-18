using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseGenerator.Models
{
public class LicenseModel
{
    public string HardwareFingerprint { get; set; }

    public DateTime ExpireDate { get; set; }

    public DateTime LastCheckDate { get; set; }

    public bool IsActive { get; set; }
}
}
