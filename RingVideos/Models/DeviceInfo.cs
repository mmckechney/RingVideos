using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eNt = KoenZomers.Ring.Api.Entities;
namespace RingVideos.Models
{
    public class DeviceInfo
    {
      public string Name { get; set; }
      public long Id { get; set; }
      public string DeviceId { get; set; }
   }

   public class DeviceList
   {
      public List<DeviceInfo> Devices { get; } = new();
      public DeviceList ExtractDevices(eNt.Devices ringDevices)
      {
         foreach(var x in ringDevices.Doorbots)
         {
            Devices.Add(new DeviceInfo() { Id  = x.Id, Name = x.Description, DeviceId = x.DeviceId });
         }
         foreach (var x in ringDevices.Chimes)
         {
            Devices.Add(new DeviceInfo() { Id = x.Id, Name = x.Description, DeviceId = x.DeviceId });
         }
         foreach (var x in ringDevices.AuthorizedDoorbots)
         {
            Devices.Add(new DeviceInfo() { Id = x.Id, Name = x.Description, DeviceId = x.DeviceId });
         }
         foreach (var x in ringDevices.StickupCams)
         {
            Devices.Add(new DeviceInfo() { Id = x.Id.Value, Name = x.Description, DeviceId = x.DeviceId });
         }

         return this;
      }

   }
}
