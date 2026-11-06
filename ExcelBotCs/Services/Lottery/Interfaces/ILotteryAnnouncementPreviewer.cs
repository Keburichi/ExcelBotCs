using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Services.Lottery.Interfaces;

/// <summary>
///     The preview seam for the admin page (#114): renders the fully assembled announcement for either result variant
///     with sample data, the live pot and the live prize pool, and returns the visible character count so the template
///     editor can show the 2,000-character budget before saving (spec §5.6, §5.9 Messages tab).
/// </summary>
public interface ILotteryAnnouncementPreviewer
{
	/// <param name="variant">One of <see cref="LotteryAnnouncementVariant" />: <c>win</c> or <c>nowin</c>.</param>
	Task<LotteryAnnouncementPreview> RenderAsync(string variant, CancellationToken cancellationToken = default);
}
