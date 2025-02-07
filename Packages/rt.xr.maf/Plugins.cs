using System;
using System.Reflection;

namespace maf {
    
    public class MediaPipelineFactoryPlugin {
        public static void RegisterAll()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes()){
                    if (type.IsSubclassOf(typeof(MediaPipelineFactoryPlugin))){
                        type.GetMethod("RegisterMediaPipelineFactoryPlugin").Invoke(null, null);
                    }
                }
            }
        }
    }

    // Declare plugins registration functions
    public class AvPipelinePlugin : MediaPipelineFactoryPlugin {
        [global::System.Runtime.InteropServices.DllImport("avpipeline")]
        public static extern void RegisterMediaPipelineFactoryPlugin();
    }

}