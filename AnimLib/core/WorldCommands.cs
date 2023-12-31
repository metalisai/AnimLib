namespace AnimLib;

internal record WorldCommand(double time);

internal record WorldSoundCommand(float volume, double time) : WorldCommand(time);

internal record WorldPlaySoundCommand(SoundSample sound, float volume, double time) : WorldSoundCommand(volume, time);

internal record WorldPropertyMultiCommand (
    int[] entityIds,
    string property,
    object newvalue,
    object[] oldvalue,
    double time
) : WorldCommand(time);

internal record WorldDynPropertyCommand (
    int entityId,
    object newvalue,
    object oldvalue,
    double time
) : WorldCommand(time);

// entity properties
internal record WorldPropertyCommand (
    int entityId,
    string property,
    object? newvalue,
    object? oldvalue,
    double time
) : WorldCommand(time);

internal record WorldCreateDynPropertyCommand (
    int propertyId,
    object value,
    double time
) : WorldCommand(time);

internal record WorldCreateCommand(object entity, double time) : WorldCommand(time);

internal record WorldDestroyCommand (int entityId, double time) : WorldCommand(time);

internal record WorldSetActiveCameraCommand(int cameraEntId, int oldCamEntId, double time) : WorldCommand(time);

internal record WorldEndCommand(double time) : WorldCommand(time);

internal record WorldCreateRenderBufferCommand(int width, int height, int id, double time) : WorldCommand(time);
