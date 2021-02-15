using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Werewolf.Game.Api;

namespace Werewolf.Game.Multiplexer
{
    public class GameApiServer : GameApiServerBase
    {
        public override Task<GameRoom?> CreateGroup(UserId request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<UserId?> GetOrCreateUser(UserCreateInfo request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<ServerState?> GetServerState(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<ActionState?> JoinGroup(GroupUserId request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<ActionState?> LeaveGroup(GroupUserId request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
