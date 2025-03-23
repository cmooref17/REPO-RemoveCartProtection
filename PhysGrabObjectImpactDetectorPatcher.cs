using HarmonyLib;
using Photon.Pun;
using REPO_RemoveCartProtection.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace REPO_RemoveCartProtection
{
    [HarmonyPatch]
    public static class PhysGrabObjectImpactDetectorPatcher
    {
        public static Dictionary<PhysGrabObjectImpactDetector, bool> inCartValues = new Dictionary<PhysGrabObjectImpactDetector, bool>(); // saving the state of the inCart variable
        public static Dictionary<PhysGrabObjectImpactDetector, PhysGrabCart> currentCartValues = new Dictionary<PhysGrabObjectImpactDetector, PhysGrabCart>(); // saving the state of the currentCart variable
        public static Dictionary<PhysGrabObjectImpactDetector, float> breakForceValues = new Dictionary<PhysGrabObjectImpactDetector, float>(); // saving the state of the breakForce variable
        public static HashSet<PhysGrabObject> grabbedObjects = new HashSet<PhysGrabObject>();
        //internal static FieldInfo isCollidingField = typeof(PhysGrabObjectImpactDetector).GetField("isColliding", BindingFlags.NonPublic | BindingFlags.Instance);


        [HarmonyPatch(typeof(PhysGrabObject), "GrabStarted")]
        [HarmonyPostfix]
        public static void OnGrabStarted(ref bool ___isValuable, PhysGrabber player, PhysGrabObject __instance)
        {
            if (GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient)
                return;

            if (___isValuable && __instance.grabbedLocal && __instance.playerGrabbing.Count > 0 && !grabbedObjects.Contains(__instance))
            {
                var valuable = __instance.GetComponent<ValuableObject>();
                if (!valuable || (valuable.physAttributePreset != null && valuable.physAttributePreset.mass >= 2 && valuable.durabilityPreset != null && valuable.durabilityPreset.fragility >= 50)) // Will not apply to slighly heavier objects that are somewhat more fragile
                    return;

                bool isBlacklisted = false;
                string rawItemName = __instance.name.ToLower();
                rawItemName = rawItemName.Replace(" ", "");
                if (!isBlacklisted)
                {
                    for (int i = 0; i < Plugin.blacklistedItemNames.Length; i++)
                    {
                        if (rawItemName.Contains(Plugin.blacklistedItemNames[i]))
                        {
                            isBlacklisted = true;
                            break;
                        }
                    }
                }
                if (!isBlacklisted)
                {
                    Plugin.LogWarningVerbose("OnGrabbed: " + __instance.name);
                    grabbedObjects.Add(__instance);
                }
            }
        }

        /*[HarmonyPatch(typeof(PhysGrabObject), "GrabEnded")]
        [HarmonyPostfix]
        public static void OnGrabEnded(ref PhysGrabObjectImpactDetector ___impactDetector, PhysGrabber player, PhysGrabObject __instance)
        {
            if (GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient)
                return;

            if (__instance.playerGrabbing.Count <= 0 && grabbedObjects.Contains(__instance))
            {
                bool isColliding = (bool)isCollidingField.GetValue(___impactDetector);
                if (isColliding)
                {
                    Plugin.LogWarningVerbose("OnGrabEnded: " + __instance.name + " - Grab ended while colliding. Removing object from Removing object from grabbedObjects.");
                    grabbedObjects.Remove(__instance);
                }
            }
        }*/




        [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "FixedUpdate")]
        [HarmonyPrefix]
        public static void OnImpactDetectorFixedUpdatePrefix(ref PhysGrabObject ___physGrabObject, ref PhysGrabCart ___currentCart, PhysGrabObjectImpactDetector __instance)
        {
            if (GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient)
                return;

            inCartValues[__instance] = __instance.inCart;
            currentCartValues[__instance] = ___currentCart;
            if (grabbedObjects.Contains(___physGrabObject) && __instance.inCart && (___currentCart || !ConfigSettings.keepImpactProtectionExtraction.Value))
            {
                __instance.inCart = false;
                ___currentCart = null;
            }
        }

        [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "FixedUpdate")]
        [HarmonyPostfix]
        public static void OnImpactDetectorFixedUpdatePostfix(ref PhysGrabObject ___physGrabObject, ref PhysGrabCart ___currentCart, ref bool ___isColliding, ref bool ___impactHappened, ref float ___breakForce, PhysGrabObjectImpactDetector __instance)
        {
            if (GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient)
                return;

            if (inCartValues.TryGetValue(__instance, out bool inCart))
                __instance.inCart = inCart;
            if (currentCartValues.TryGetValue(__instance, out PhysGrabCart currentCart))
                ___currentCart = currentCart;
        }




        [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "OnCollisionStay")]
        [HarmonyPrefix]
        public static void OnCollisionStayPrefix(ref PhysGrabObject ___physGrabObject, ref PhysGrabCart ___currentCart, ref bool ___collisionsActive, ref bool ___isMoving, ref bool ___impactHappened, ref float ___breakForce, ref float ___impactLevel1, ref float ___indestructibleSpawnTimer, Collision collision, PhysGrabObjectImpactDetector __instance)
        {
            if ((GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient) || !___collisionsActive || !___isMoving)
                return;

            inCartValues[__instance] = __instance.inCart;
            currentCartValues[__instance] = ___currentCart;
            breakForceValues[__instance] = ___breakForce;
            if (grabbedObjects.Contains(___physGrabObject) && __instance.inCart && ___impactHappened && ___indestructibleSpawnTimer <= 0)
            {
                __instance.inCart = false;
                ___currentCart = null;
                float oldBreakForce = ___breakForce;
                ___breakForce = ___breakForce * ConfigSettings.cartImpactMultiplier.Value;

                if (___breakForce < ___impactLevel1 && oldBreakForce >= ___impactLevel1)
                    Plugin.LogWarningVerbose("Prevented impact damage. BreakForce reduced in cart: " + oldBreakForce + " => " + ___breakForce);
                else if (oldBreakForce >= 50)
                    Plugin.LogWarningVerbose("BreakForce reduced in cart: " + oldBreakForce + " => " + ___breakForce);
            }
        }

        [HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "OnCollisionStay")]
        [HarmonyPostfix]
        public static void OnCollisionStayPostfix(ref PhysGrabObject ___physGrabObject, ref PhysGrabCart ___currentCart, ref bool ___collisionsActive, ref bool ___isMoving, ref bool ___impactHappened, ref float ___breakForce, Collision collision, PhysGrabObjectImpactDetector __instance)
        {
            if ((GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient) || !___collisionsActive || !___isMoving)
                return;

            if (inCartValues.TryGetValue(__instance, out bool inCart))
                __instance.inCart = inCart;
            if (currentCartValues.TryGetValue(__instance, out PhysGrabCart currentCart))
                ___currentCart = currentCart;
            if (breakForceValues.TryGetValue(__instance, out float breakForce))
                ___breakForce = breakForce;

            if (grabbedObjects.Contains(___physGrabObject) && ___physGrabObject.playerGrabbing.Count <= 0)
            {
                Plugin.LogWarningVerbose("Collided with object while not grabbed by a player. Removing object from grabbedObjects: " + ___physGrabObject.name);
                grabbedObjects.Remove(___physGrabObject);
            }
        }
    }
}
