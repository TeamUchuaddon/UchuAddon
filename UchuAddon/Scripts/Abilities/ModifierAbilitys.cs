using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Assignable;

namespace Hori.Scripts.Abilities;

public class IgnoreWallVisionAbility : AbstractPlayerAbility, IPlayerAbility
{
    public IgnoreWallVisionAbility(GamePlayer player) : base(player) {  }

    bool IPlayerAbility.EyesightIgnoreWalls => true;
}

public class IgnoreBlackoutVisionAbility : AbstractPlayerAbility, IPlayerAbility
{
    public IgnoreBlackoutVisionAbility(GamePlayer player) : base(player) { }

    bool IPlayerAbility.IgnoreBlackout => true;
}

public class MeetingButtonBlockAbility : AbstractPlayerAbility, IPlayerAbility
{
    public MeetingButtonBlockAbility(GamePlayer player) : base(player) { }

    bool IPlayerAbility.BlockCallingEmergencyMeeting => true;
}

public class ClearVisionAbility : AbstractPlayerAbility, IPlayerAbility
{
    public ClearVisionAbility(GamePlayer player) : base(player) { }

    bool IPlayerAbility.EyesightIgnoreWalls => true;
}





