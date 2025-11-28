using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Pulse;

public sealed record CreatePulseCommand(
    Guid AuthorId,
    string Content,
    Stream? ImageStream = null
) : ICommand<CreatePulseResult>;

public sealed record CreatePulseResult(
    Guid PulseId,
    string Content,
    DateTime CreatedAt,
    string? ImageUrl = null
);

public sealed class CreatePulseCommandHandler
    : ICommandHandler<CreatePulseCommand, CreatePulseResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPulseImageService _pulseImageService;

    public CreatePulseCommandHandler(
        IUnitOfWork unitOfWork,
        IPulseImageService pulseImageService)
    {
        _unitOfWork = unitOfWork;
        _pulseImageService = pulseImageService;
    }

    public async Task<CreatePulseResult> HandleAsync(
        CreatePulseCommand command,
        CancellationToken cancellationToken)
    {
        // Create Pulse entity
        var pulse = Domain.Pulse.Pulse.Create(
            authorId: command.AuthorId,
            content: command.Content);

        _unitOfWork.Pulses.Create(pulse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create PulseEngagement with zero counts
        var engagement = PulseEngagement.Create(pulse.Id);
        _unitOfWork.Pulses.CreateEngagement(engagement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Process and save image if provided
        string? imageUrl = null;
        if (command.ImageStream is not null)
        {
            var (url, width, height) = await _pulseImageService.SavePulseImageAsync(
                command.ImageStream,
                pulse.Id,
                cancellationToken);

            var pulseImage = PulseImage.Create(
                pulseId: pulse.Id,
                url: url,
                width: width,
                height: height);

            _unitOfWork.Pulses.CreateImage(pulseImage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            imageUrl = url;
        }

        return new CreatePulseResult(
            PulseId: pulse.Id,
            Content: pulse.Content,
            CreatedAt: pulse.CreatedAt,
            ImageUrl: imageUrl
        );
    }
}
