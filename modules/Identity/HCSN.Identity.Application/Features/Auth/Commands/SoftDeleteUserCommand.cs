using HCSN.Identity.Domain.Interfaces;
using MediatR;

namespace HCSN.Identity.Application.Features.Users.Commands;

public record SoftDeleteUserCommand(Guid UserId) : IRequest<bool>;

public class SoftDeleteUserCommandHandler : IRequestHandler<SoftDeleteUserCommand, bool>
{
    private readonly IUserRepository _userRepository;

    public SoftDeleteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(
        SoftDeleteUserCommand request,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            return false;

        await _userRepository.SoftDeleteAsync(request.UserId);
        return true;
    }
}
