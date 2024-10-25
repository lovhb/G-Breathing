﻿using HarmonyLib;
using UnityEngine.Audio;

namespace GBreathing
{
    [HarmonyPatch(typeof(Actor), "Start")]
    internal class PlayerSpawnPatch
    {
        public static void Postfix(Actor instance)
        {
            if (instance.GetComponent<PlayerEntityIdentifier>() == null ||
                instance.gameObject.GetComponentInChildren<VehicleMaster>(true).playerVehicle == null)
                return;

            FlightInfo flightInfo = instance.GetComponent<FlightInfo>();
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