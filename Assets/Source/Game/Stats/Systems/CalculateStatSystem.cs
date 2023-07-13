using Rogue.Utilities;
using Unity.Collections;
using Wargon.Ecsape;

namespace Rogue {
    public class CalculateStatSystem<TStatComponent> : ISystem, IOnCreate where TStatComponent : struct, IComponent {
        private Query _changedStats;
        private Query _removedStats;
        private Query _additiveStats;
        private Query _multiplyStats;
        public void OnCreate(World world) {
            _changedStats = world.GetQuery().WithAll(
                typeof(TStatComponent), 
                typeof(StatsChangedEvent), 
                typeof(StatReceiverLink));
            _removedStats = world.GetQuery().WithAll(
                typeof(TStatComponent), 
                typeof(StatsRemovedEvent), 
                typeof(StatReceiverLink));
            _additiveStats = world.GetQuery().WithAll(
                typeof(TStatComponent), 
                typeof(AdditiveStatTag), 
                typeof(StatReceiverLink));
            _multiplyStats = world.GetQuery().WithAll(
                typeof(TStatComponent), 
                typeof(MultiplyStatTag), 
                typeof(StatReceiverLink));
        }
        public void OnUpdate(float deltaTime) {
            foreach (ref var changedStat in _changedStats) {
                ref var statReceiver = ref changedStat.Get<StatReceiverLink>();
                Calculate(in statReceiver);
            }
        }

        private void Calculate(in StatReceiverLink statReceiver) {

            ref var receiverStat = ref statReceiver.Value.Get<TStatComponent>();

            var totalStatValue = 0f;
            foreach (ref var childStatEntity in _additiveStats) {
                ref var stat = ref childStatEntity.Get<TStatComponent>();
                ref var value = ref InterpretUnsafeUtility.Retrieve<TStatComponent, float>(ref stat);
                totalStatValue += value;
                
            }

            foreach (ref var multiplyStat in _multiplyStats) {
                ref var stat = ref multiplyStat.Get<TStatComponent>();
                ref var value = ref InterpretUnsafeUtility.Retrieve<TStatComponent, float>(ref stat);
                totalStatValue *= value;
            }

            ref var receiverStatValue = ref InterpretUnsafeUtility.Retrieve<TStatComponent, float>(ref receiverStat);
            receiverStatValue = totalStatValue;

            statReceiver.Value.Add(new StatRecievedElementEvent() {
                StatType = Component<TStatComponent>.Index
            });
            statReceiver.Value.Set(receiverStat);
        }
    }
}