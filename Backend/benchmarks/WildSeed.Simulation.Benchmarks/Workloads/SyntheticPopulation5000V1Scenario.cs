namespace WildSeed.Simulation.Benchmarks.Workloads;

public sealed class SyntheticPopulation5000V1Scenario
{
    private readonly int _population;
    private readonly int[] _agentId;
    private readonly int[] _readX;
    private readonly int[] _readY;
    private readonly int[] _readEnergy;
    private readonly int[] _writeX;
    private readonly int[] _writeY;
    private readonly int[] _writeEnergy;

    private readonly int[] _cellHead;
    private readonly int[] _cellNext;

    private ulong _rngState;

    public SyntheticPopulation5000V1Scenario(
        ulong seed = SyntheticPopulation5000V1Configuration.DefaultSeed,
        int population = SyntheticPopulation5000V1Configuration.PopulationSize)
    {
        _population = population;
        _rngState = seed == 0 ? 0x853c49e6748fea9bUL : seed;

        _agentId = new int[population];
        _readX = new int[population];
        _readY = new int[population];
        _readEnergy = new int[population];
        _writeX = new int[population];
        _writeY = new int[population];
        _writeEnergy = new int[population];

        int totalCells = SyntheticPopulation5000V1Configuration.GridCols * SyntheticPopulation5000V1Configuration.GridRows;
        _cellHead = new int[totalCells];
        _cellNext = new int[population];

        for (int i = 0; i < population; i++)
        {
            _agentId[i] = i + 1;
            _readX[i] = (int)(NextRandom() % (ulong)SyntheticPopulation5000V1Configuration.WorldWidth);
            _readY[i] = (int)(NextRandom() % (ulong)SyntheticPopulation5000V1Configuration.WorldHeight);
            _readEnergy[i] = 50 + (int)(NextRandom() % 50);
        }
    }

    public long Step(int tickCount)
    {
        long checksum = 0;

        for (int t = 0; t < tickCount; t++)
        {
            Array.Fill(_cellHead, -1);
            for (int i = 0; i < _population; i++)
            {
                int col = Math.Clamp(_readX[i] / SyntheticPopulation5000V1Configuration.GridCellSize, 0, SyntheticPopulation5000V1Configuration.GridCols - 1);
                int row = Math.Clamp(_readY[i] / SyntheticPopulation5000V1Configuration.GridCellSize, 0, SyntheticPopulation5000V1Configuration.GridRows - 1);
                int cellIdx = row * SyntheticPopulation5000V1Configuration.GridCols + col;

                _cellNext[i] = _cellHead[cellIdx];
                _cellHead[cellIdx] = i;
            }

            for (int i = 0; i < _population; i++)
            {
                int x = _readX[i];
                int y = _readY[i];
                int energy = _readEnergy[i];

                int col = Math.Clamp(x / SyntheticPopulation5000V1Configuration.GridCellSize, 0, SyntheticPopulation5000V1Configuration.GridCols - 1);
                int row = Math.Clamp(y / SyntheticPopulation5000V1Configuration.GridCellSize, 0, SyntheticPopulation5000V1Configuration.GridRows - 1);
                int cellIdx = row * SyntheticPopulation5000V1Configuration.GridCols + col;

                int neighborCount = 0;
                int currentNeighbor = _cellHead[cellIdx];
                while (currentNeighbor != -1 && neighborCount < 8)
                {
                    if (currentNeighbor != i)
                    {
                        neighborCount++;
                    }
                    currentNeighbor = _cellNext[currentNeighbor];
                }

                int dx = (int)(NextRandom() % 7) - 3;
                int dy = (int)(NextRandom() % 7) - 3;
                int energyCost = 1 + (neighborCount > 3 ? 1 : 0);

                int nx = Math.Clamp(x + dx, 0, SyntheticPopulation5000V1Configuration.WorldWidth - 1);
                int ny = Math.Clamp(y + dy, 0, SyntheticPopulation5000V1Configuration.WorldHeight - 1);
                int ne = Math.Clamp(energy - energyCost + (neighborCount == 0 ? 2 : 0), 0, 100);

                _writeX[i] = nx;
                _writeY[i] = ny;
                _writeEnergy[i] = ne;

                checksum += nx + ny + ne;
            }

            Array.Copy(_writeX, _readX, _population);
            Array.Copy(_writeY, _readY, _population);
            Array.Copy(_writeEnergy, _readEnergy, _population);
        }

        return checksum;
    }

    private ulong NextRandom()
    {
        _rngState ^= _rngState >> 12;
        _rngState ^= _rngState << 25;
        _rngState ^= _rngState >> 27;
        return _rngState * 0x2545F4914F6CDD1DUL;
    }
}
