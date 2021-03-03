using System.Collections.Generic;
using Werewolf.Theme;

namespace Avalon.The.Resistance
{
    public abstract class BaseRole : Role
    {
        protected BaseRole(Theme theme) : base(theme)
        {
        }

        public override string Name => GetType().Name;

        public bool IsMissionLeader { get; set; }

        public bool IsSelectedByLeader { get; set; }

        public bool? HasAcceptedRequest { get; set; }

        public override IEnumerable<string> GetTags(GameRoom game, Role? viewer)
        {
            foreach (var tag in base.GetTags(game, viewer))
                yield return tag;
            if (IsMissionLeader)
                yield return "mission-leader";
            if (IsSelectedByLeader)
                yield return "select-for-mission";
            if (viewer == this)
            {
                if (HasAcceptedRequest == true)
                    yield return "request-accepted";
                if (HasAcceptedRequest == false)
                    yield return "request-denied";
            }
        }
    }
}