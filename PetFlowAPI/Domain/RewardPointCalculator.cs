using System;

namespace PetFlowAPI.Domain
{
    /// <summary>
    /// Regra de negócio para cálculo de pontos de recompensa de um evento de saúde,
    /// conforme o multiplicador do plano (<c>plan.points_per_event</c>) e os pontos
    /// base do tipo de evento (<c>event_type.points_reward</c>) definidos no DDL Oracle.
    /// </summary>
    public interface IRewardPointCalculator
    {
        /// <summary>
        /// Calcula o total de pontos a conceder a um tutor quando um evento de saúde
        /// é marcado como concluído (status REALIZADO).
        /// </summary>
        /// <param name="eventTypeBasePoints">Pontos base do tipo de evento (event_type.points_reward).</param>
        /// <param name="planPointsMultiplier">Multiplicador do plano do pet (plan.points_per_event).</param>
        int Calculate(int eventTypeBasePoints, int planPointsMultiplier);
    }

    public sealed class RewardPointCalculator : IRewardPointCalculator
    {
        public int Calculate(int eventTypeBasePoints, int planPointsMultiplier)
        {
            if (eventTypeBasePoints < 0)
                throw new ArgumentOutOfRangeException(nameof(eventTypeBasePoints), "Pontos base do evento não podem ser negativos.");

            if (planPointsMultiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(planPointsMultiplier), "Multiplicador do plano deve ser maior que zero.");

            return eventTypeBasePoints * planPointsMultiplier;
        }
    }
}
