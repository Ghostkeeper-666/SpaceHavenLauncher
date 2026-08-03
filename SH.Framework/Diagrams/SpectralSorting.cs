using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Linq;

namespace SH.Framework.Diagrams;

public static class SpectralSorting
{
    /// <summary>
    /// Returns indices ordered by spectral seriation. Affinity[i,j] must be symmetric.
    /// </summary>
    public static int[] Order(double[,] affinity)
    {
        int n = affinity.GetLength(0);

        Matrix<double> A = Matrix.Build.DenseOfArray(affinity);

        // Degree matrix
        Matrix<double> D = Matrix.Build.DenseDiagonal(n, n, i =>
        {
            double sum = 0;
            for (int j = 0; j < n; j++)
                sum += affinity[i, j];
            return sum;
        });

        // Unnormalized Laplacian
        Matrix<double> L = D - A;

        // Eigen decomposition
        Evd<double> evd = L.Evd(Symmetricity.Symmetric);

        // Pair eigenvalues with eigenvectors

        // Second smallest eigenvector (Fiedler vector)
        Vector<double> fiedler =
            Enumerable.Range(0, n)
            .Select(i => new
            {
                Value = evd.EigenValues[i].Real,
                Vector = evd.EigenVectors.Column(i)
            })
            .OrderBy(p => p.Value)
            .ToList()[1].Vector;

        return
            Enumerable.Range(0, n)
            .OrderBy(i => fiedler[i])
            .ToArray();
    }
}