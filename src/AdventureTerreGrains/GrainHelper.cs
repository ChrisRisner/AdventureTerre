using AdventureTerreInterfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.GrainInterfaces;
using AdventureTerreInterfaces.Models;

namespace AdventureTerreGrains
{
    public class GrainHelper
    {
        public static string GetPrimaryKeyStringFromGrain(IGrain grain)
        {
            //TODO: Check to make sure grain is of type IGrainWithStringKey
            string stringPrimaryKey; 
            Guid guidPrimaryKey = grain.GetPrimaryKey(out stringPrimaryKey);
            return stringPrimaryKey;
        }

        async internal static Task<string> GetDescriptorForState(IGameStateGrain gameState, List<Descriptor> descriptors, IPlayerGrain playerGrain)
        {
            if (descriptors != null && descriptors.Count > 0)
            {
                if (descriptors.Count == 1)
                {
                    if (descriptors[0].SetFlags != null &&
                                    descriptors[0].SetFlags.Count > 0)
                    {
                        await gameState.SetGameStateFlags(descriptors[0].SetFlags);
                    }
                    return descriptors[0].Text;
                }
                else
                {
                    foreach (var descriptor in descriptors)
                    {
                        if (descriptor.IsDefault != true)
                        {
                            bool conditionsFailed = false;
                            foreach (var flag in descriptor.Flags)
                            {
                                var gs = GrainFactory.GetGrain<IGameStateGrain>(playerGrain.GetPrimaryKey());
                                if ((await gs.GetStateForKey(flag.Key)) != flag.Value)
                                    conditionsFailed = true;
                            }
                            if (conditionsFailed == false)
                            {
                                if (descriptor.SetFlags != null &&
                                    descriptor.SetFlags.Count > 0) { 
                                    await gameState.SetGameStateFlags(descriptor.SetFlags);
                                }
                                return descriptor.Text;
                                //return descriptor;
                            }
                            else
                            {
                                if (descriptors[0].SetFlags != null &&
                                    descriptors[0].SetFlags.Count > 0)
                                {
                                    try
                                    {
                                        await gameState.SetGameStateFlags(descriptors[0].SetFlags);
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("Error setting game state flags: " + ex.Message);
                                        return "error";
                                    }
                                }
                                return descriptors[0].Text;
                            }
                        }
                    }
                }
            }
            return "No Descriptors";
        }
    }
}
