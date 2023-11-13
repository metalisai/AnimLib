namespace AnimLib;

internal class WorldCommand {
    public double time;
}

internal class WorldSoundCommand : WorldCommand {
    public float volume;
}

internal class WorldPlaySoundCommand : WorldSoundCommand {
    public SoundSample sound;
}

internal class WorldPropertyMultiCommand : WorldCommand {
    public int[] entityIds;
    public string property;
    public object newvalue;
    public object[] oldvalue;
}

internal class WorldPropertyCommand : WorldCommand {
    public int entityId;
    public string property;
    public object newvalue;
    public object oldvalue;
}

internal class WorldCreateCommand : WorldCommand {
    public object entity;
}

internal class WorldDestroyCommand : WorldCommand {
    public int entityId;
}

internal class WorldAbsorbCommand : WorldCommand {
    public int entityId;
    public float progress;
    public float oldprogress;
    public Vector3? absorbPoint;
    public Vector3? absorbScreenPoint;
}

internal class WorldSetActiveCameraCommand : WorldCommand {
    public int cameraEntId;
    public int oldCamEntId;
}

internal class WorldEndCommand : WorldCommand {
    
}

internal class WorldCreateRenderBufferCommand : WorldCommand {
    public int width;
    public int height;
    public int Id;
}
