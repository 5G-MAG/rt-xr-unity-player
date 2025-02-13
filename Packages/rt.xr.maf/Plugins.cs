using System;
using System.Reflection;

namespace maf {
    
    public class MediaPipelineFactoryPlugin {
        public static void RegisterAll()
        {
            // Manually call plugin registration
            AvPipelinePlugin.RegisterMediaPipelineFactoryPlugin();
            
            /* auto registration doesn't seem to work on all platforms ...

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes()){
                    if (type.IsSubclassOf(typeof(MediaPipelineFactoryPlugin))){
                        type.GetMethod("RegisterMediaPipelineFactoryPlugin").Invoke(null, null);
                    }
                }
            }
            */
        }
    }

    // Declare plugins registration functions
    public class AvPipelinePlugin : MediaPipelineFactoryPlugin {
        // a plugin library named `avpipeline` is expected in rt.xr.maf/bin for each supported platform
        [global::System.Runtime.InteropServices.DllImport("avpipeline")]
        public static extern void RegisterMediaPipelineFactoryPlugin();
    }

}