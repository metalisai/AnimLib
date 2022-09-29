
namespace AnimLib {
    public class WorldCommand {
        public double time;
    }

    public class WorldSoundCommand : WorldCommand {
        public float volume;
    }

    public class WorldPlaySoundCommand : WorldSoundCommand {
        public SoundSample sound;
    }

    public class WorldPropertyMultiCommand : WorldCommand {
        public int[] entityIds;
        public string property;
        public object newvalue;
        public object[] oldvalue;
    }

    public class WorldPropertyCommand : WorldCommand {
        public int entityId;
        public string property;
        public object newvalue;
        public object oldvalue;
    }

    public class WorldCreateCommand : WorldCommand {
        public object entity;
    }

    public class WorldDestroyCommand : WorldCommand {
        public int entityId;
    }

    public class WorldAbsorbCommand : WorldCommand {
        public int entityId;
        public float progress;
        public float oldprogress;
        public Vector3? absorbPoint;
        public Vector3? absorbScreenPoint;
    }

    public class WorldSetActiveCameraCommand : WorldCommand {
        public int cameraEntId;
        public int oldCamEntId;
    }

    public class WorldEndCommand : WorldCommand {
        
    }
}
