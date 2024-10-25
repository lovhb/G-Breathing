using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using VTOLAPI;

#nullable disable
namespace GBreathing
{
    [HarmonyPatch(typeof(Actor), "Start")]
    internal class PlayerSpawnPatch
    {
        public static void Postfix(Actor __instance)
        {
            if (__instance.GetComponent<PlayerEntityIdentifier>() == null ||
                __instance.gameObject.GetComponentInChildren<VehicleMaster>(true).playerVehicle == null)
                return;

            FlightInfo flightInfo = __instance.GetComponent<FlightInfo>();
            if (flightInfo != null)
            {
                Main.SetFlightInfo(flightInfo);
            }

            // Retrieve the AudioMixerGroup using VTResources and set it in the Main class
            AudioMixerGroup interiorMixerGroup = VTResources.GetInteriorMixerGroup();
            if (interiorMixerGroup != null)
            {
                Main.SetMixerGroup(interiorMixerGroup);
            }
        }
    }
}