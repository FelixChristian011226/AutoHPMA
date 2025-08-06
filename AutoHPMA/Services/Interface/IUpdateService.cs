using AutoHPMA.Models;

namespace AutoHPMA.Services.Interface
{
    public interface IUpdateService
    {
        Task CheckUpdateAsync(UpdateOption option);
    }
}