using System.Linq;
using Hardware.Info;

namespace G2.Service.Helper
{
    public class SystemLoad
    {
        private readonly HardwareInfo _hardwareInfo;

        public SystemLoad()
        {
            _hardwareInfo = new HardwareInfo();
        }

        public List<float> GetCpuUsage()
        {
            return [.5f, .2f, .8f];

        }

        public long GetMemoryUsage()
        {
            _hardwareInfo.RefreshMemoryStatus();
            ulong totalRam = _hardwareInfo.MemoryStatus.TotalPhysical;
            ulong availableRam = _hardwareInfo.MemoryStatus.AvailablePhysical;

            return (long) (totalRam - availableRam);
        }

        public long GetTotalMemory()
        {
            _hardwareInfo.RefreshMemoryStatus();
            ulong totalRam = _hardwareInfo.MemoryStatus.TotalPhysical;
            return (long)totalRam;
        }

        public long GetStorageUsed()
        {
            string rootPath = "/";
            DriveInfo drive = new(rootPath);

            return drive.TotalSize - drive.AvailableFreeSpace;
        }

        public long GetTotalStorage()
        {
            string rootPath = "/";
            DriveInfo drive = new(rootPath);
            return drive.TotalSize;
        }
    }
}