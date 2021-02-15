namespace Werewolf.Theme
{
    public enum KillState : byte
    {
        /// <summary>
        /// This role is alive.
        /// </summary>
        Alive = 0,
        /// <summary>
        /// This role is marked to be killed. This flag can be changed back to <see cref="Alive"/>.
        /// <br/>
        /// Only certain roles see a flag.
        /// <br/>
        /// This state contains a <see cref="KillInfo"/>.
        /// </summary>
        MarkedKill = 1,
        /// <summary>
        /// This state searches for connected victims and prepares some operations. In the
        /// transition to <see cref="BeforeKill"/> all notifications will be sent.
        /// <br/>
        /// Everyone see this role as killed.
        /// <br/>
        /// This state contains a <see cref="KillInfo"/>.
        /// </summary>
        AboutToKill = 2,
        /// <summary>
        /// The role can do some last stuff.
        /// <br/>
        /// Everyone see this role as killed.
        /// <br/>
        /// This state contains a <see cref="KillInfo"/>.
        /// </summary>
        BeforeKill = 3,
        /// <summary>
        /// The role is finally killed and can't do anything.
        /// <br/>
        /// Everyone see this role as killed.
        /// <br/>
        /// This state doesn't contains a <see cref="KillInfo"/>.
        /// </summary>
        Killed = 4,
    }
}
