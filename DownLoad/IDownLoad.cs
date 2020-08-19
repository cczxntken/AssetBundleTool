using System.Threading.Tasks;

namespace LitEngine.LoadAsset.DownLoad
{
    public interface IDownLoad : System.IDisposable
    {
        bool IsDone { get; }
        void StartAsync();
        void Update();
    }
}