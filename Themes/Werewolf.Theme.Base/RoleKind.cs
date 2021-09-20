using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Werewolf.Theme
{
    public readonly struct RoleKind
    {
        readonly OneOf<Leader, Player, Spectator> data;

        public RoleKind(Leader leader)
        {
            data = leader;
        }

        public static RoleKind CreateLeader() => new RoleKind(new Leader());

        public RoleKind(Spectator spectator)
        {
            data = spectator;
        }

        public static RoleKind CreateSpectator() => new RoleKind(new Spectator());

        public RoleKind(Player player)
        {
            data = player;
        }

        public static RoleKind CreatePlayer(Role role) => new RoleKind(role);

        public RoleKind(Role role)
        {
            data = new Player(role);
        }

        public struct Leader
        {
            public override int GetHashCode()
            {
                return 0x6490E3DF;
            }

            public override bool Equals(object? obj)
            {
                return obj is Leader;
            }
        }

        public struct Spectator
        {
            public override int GetHashCode()
            {
                return 0x3DB89EEE;
            }

            public override bool Equals(object? obj)
            {
                return obj is Spectator;
            }
        }

        public struct Player
        {
            public Role Role { get; }

            public Player(Role role) => Role = role;

            public static implicit operator Role(Player player)
                => player.Role;

            public static implicit operator Player(Role role)
                => new Player(role);

            public override bool Equals(object? obj)
            {
                return obj is Player player &&
                       EqualityComparer<Role>.Default.Equals(Role, player.Role);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Role);
            }

            public static bool operator ==(Player left, Player right)
                => left.Role == right.Role;

            public static bool operator !=(Player left, Player right)
                => left.Role != right.Role;
        }
    
        public readonly bool IsLeader => data.IsT0;
        public readonly bool IsPlayer => data.IsT1;
        public readonly bool IsSpectator => data.IsT2;

        public readonly bool IsLeaderOrRole<T>() 
            where T : Role
            => IsLeader || AsPlayer is T;

        public readonly bool IsLeaderOrRole<T1, T2>() 
            where T1 : Role
            where T2 : Role
            => IsLeader || AsPlayer is T1 or T2;

        public readonly bool IsLeaderOrRole<T1, T2, T3>() 
            where T1 : Role
            where T2 : Role
            where T3 : Role
            => IsLeader || AsPlayer is T1 or T2 or T3;

        public readonly Role? AsPlayer => IsPlayer ? data.AsT1.Role : null;
    
        public readonly void Switch(Action funcLeader, Action<Role> funcPlayer, Action funcSpectator)
        {
            data.Switch(
                _ => funcLeader(),
                x => funcPlayer(x.Role),
                _ => funcSpectator()
            );
        }

        public readonly T Match<T>(Func<T> funcLeader, Func<Role, T> funcPlayer, Func<T> funcSpectator)
        {
            return data.Match(
                _ => funcLeader(),
                x => funcPlayer(x.Role),
                _ => funcSpectator()
            );
        }

        public readonly bool TryGetPlayer([NotNullWhen(true)] out Role? player)
        {
            var success = data.TryPickT1(out Player x, out _);
            player = success ? x.Role : null;
            return success;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is RoleKind kind &&
                   EqualityComparer<OneOf<Leader, Player, Spectator>>.Default.Equals(data, kind.data);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(data);
        }
    }
}