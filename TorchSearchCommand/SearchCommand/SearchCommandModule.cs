using NLog;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace SearchCommand
{
    public sealed partial class SearchCommandModule
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        void DisplayGps(IMyEntity entity)
        {
            var gpsCollection = (MyGpsCollection) MyAPIGateway.Session?.GPS;
            if (gpsCollection == null)
            {
                Context.Respond("GPS not available.", Color.Red);
                return;
            }

            var gps = new MyGps
            {
                Coords = entity.GetPosition(),
                Name = $"!sp {entity.DisplayName}",
                GPSColor = Color.Green,
                IsContainerGPS = true,
                ShowOnHud = true,
                DiscardAt = null,
            };

            gps.UpdateHash();
            gps.SetEntity(entity);

            gpsCollection.SendAddGps(Context.Player.IdentityId, ref gps, entity.EntityId);
        }
    }
}