namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionDecalibration
    {
        public static readonly ProductionDecalibration Default = new ProductionDecalibration(0.005, 0.003, 1);

        public readonly double distorsionMin;
        public readonly double distorsionMax;
        public readonly double decrease;

        public ProductionDecalibration(double distortionMin, double distortionMax, double decrease)
        {
            distorsionMax = distortionMax;
            distorsionMin = distortionMin;
            this.decrease = decrease;
        }

        public double DistortionMultiplier()
        {
            return 1 - FastRandom.NextDouble(distorsionMin, distorsionMax);
        }
    }
}