using FluentValidation;

namespace WMSP.Api.Models.Dtos;

public class CreatePlanValidator : AbstractValidator<CreatePlanDto>
{
    public CreatePlanValidator()
    {
        RuleFor(x => x.PlanName).NotEmpty().WithMessage("计划名称不能为空").MaximumLength(200);
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("请选择目标仓库");
        RuleFor(x => x.PlanType).NotEmpty().WithMessage("请选择盘点类型")
            .Must(x => x is "FULL" or "ZONE" or "SPOT").WithMessage("无效的盘点类型");
        RuleFor(x => x.CheckMode).NotEmpty().WithMessage("请选择盘点模式")
            .Must(x => x is "BLIND" or "OPEN").WithMessage("无效的盘点模式");
        RuleFor(x => x.PlanDate).NotEmpty().WithMessage("请选择计划日期")
            .Must(d => DateOnly.TryParse(d, out var date) && date >= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("计划日期不能早于今天");
        RuleFor(x => x.Zones)
            .Must(z => z != null && z.Count > 0)
            .When(x => x.PlanType == "ZONE")
            .WithMessage("区域盘时必须选择至少一个区域");
        RuleFor(x => x.SampleRate)
            .NotNull().When(x => x.PlanType == "SPOT")
            .WithMessage("抽盘时必须填写抽盘比例");
        RuleFor(x => x.SampleRate)
            .InclusiveBetween(1, 100).When(x => x.PlanType == "SPOT" && x.SampleRate.HasValue)
            .WithMessage("抽盘比例必须在1-100之间");
        RuleFor(x => x.Remark).MaximumLength(500);
    }
}
