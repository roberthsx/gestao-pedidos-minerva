using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Domain.Events;
using Minerva.GestaoPedidos.Domain.ReadModels;

namespace Minerva.GestaoPedidos.Application.Common.Mappings;

/// <summary>
/// Perfil AutoMapper para conversões de Command, Domain, ReadModel e DTO.
/// Padrão: DTOs simples e entidade → ReadModel usam AutoMapper (produtividade e consistência com Handlers).
/// Projeção direta (Select no EF) permanece em CustomerReadRepository e PaymentConditionReadRepository por performance.
/// Mapeamento manual só onde houver lógica de domínio complexa que AutoMapper não resolva bem.
/// </summary>
public class MappingProfile : AutoMapper.Profile
{
    public MappingProfile()
    {
        // User -> UserDto (API response)
        CreateMap<User, UserDto>();

        // User -> UserReadModel (read side); CreatedAtUtc required for read model
        CreateMap<User, UserReadModel>()
            .ForMember(d => d.CreatedAtUtc, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // UserReadModel -> UserDto (query responses)
        CreateMap<UserReadModel, UserDto>();

        // UserCreatedEvent -> UserReadModel (sync to read side; same property names)
        CreateMap<UserCreatedEvent, UserReadModel>();

        // OrderReadModel -> OrderDto for API responses (record exige ForCtorParam + ForMember)
        CreateMap<OrderItemReadModel, OrderItemDto>();

        CreateMap<OrderReadModel, OrderDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(s => s.OrderId))
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.OrderId))
            .ForCtorParam("ApprovedBy", opt => opt.MapFrom(s => s.ApprovedBy))
            .ForMember(d => d.ApprovedBy, opt => opt.MapFrom(s => s.ApprovedBy))
            .ForCtorParam("ApprovedAt", opt => opt.MapFrom(s => s.ApprovedAt))
            .ForMember(d => d.ApprovedAt, opt => opt.MapFrom(s => s.ApprovedAt))
            .ForCtorParam("Items", opt => opt.MapFrom(s => s.Items ?? new List<OrderItemReadModel>()))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items ?? new List<OrderItemReadModel>()));

        // Order -> OrderReadModel (read side; usado por OrderReadRepository e Fakes)
        CreateMap<OrderItem, OrderItemReadModel>();
        CreateMap<Order, OrderReadModel>()
            .ForMember(d => d.OrderId, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : string.Empty))
            .ForMember(d => d.PaymentConditionDescription, opt => opt.MapFrom(s => s.PaymentCondition != null ? s.PaymentCondition.Description : string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.DeliveryDays, opt => opt.MapFrom(s => s.DeliveryTerm != null ? s.DeliveryTerm.DeliveryDays : 0))
            .ForMember(d => d.EstimatedDeliveryDate, opt => opt.MapFrom(s => s.DeliveryTerm != null ? s.DeliveryTerm.EstimatedDeliveryDate : (DateTime?)null))
            .ForMember(d => d.CreatedAtUtc, opt => opt.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.ApprovedBy, opt => opt.MapFrom(s => s.ApprovedBy))
            .ForMember(d => d.ApprovedAt, opt => opt.MapFrom(s => s.ApprovedAt))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        // Order (entity com Customer, PaymentCondition, DeliveryTerm) -> OrderDto. Navegações nulas (fallback sem Include) usam string vazia / 0 / null.
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<Order, OrderDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
            .ForCtorParam("CustomerName", opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : string.Empty))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer != null ? s.Customer.Name : string.Empty))
            .ForCtorParam("PaymentConditionDescription", opt => opt.MapFrom(s => s.PaymentCondition != null ? s.PaymentCondition.Description : string.Empty))
            .ForMember(d => d.PaymentConditionDescription, opt => opt.MapFrom(s => s.PaymentCondition != null ? s.PaymentCondition.Description : string.Empty))
            .ForCtorParam("Status", opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForCtorParam("DeliveryDays", opt => opt.MapFrom(s => s.DeliveryTerm != null ? s.DeliveryTerm.DeliveryDays : 0))
            .ForMember(d => d.DeliveryDays, opt => opt.MapFrom(s => s.DeliveryTerm != null ? s.DeliveryTerm.DeliveryDays : 0))
            .ForCtorParam("EstimatedDeliveryDate", opt => opt.MapFrom(s => s.DeliveryTerm != null ? s.DeliveryTerm.EstimatedDeliveryDate : (DateTime?)null))
            .ForMember(d => d.EstimatedDeliveryDate, opt => opt.MapFrom(s => s.DeliveryTerm != null ? s.DeliveryTerm.EstimatedDeliveryDate : (DateTime?)null))
            .ForCtorParam("ApprovedBy", opt => opt.MapFrom(s => s.ApprovedBy))
            .ForMember(d => d.ApprovedBy, opt => opt.MapFrom(s => s.ApprovedBy))
            .ForCtorParam("ApprovedAt", opt => opt.MapFrom(s => s.ApprovedAt))
            .ForMember(d => d.ApprovedAt, opt => opt.MapFrom(s => s.ApprovedAt))
            .ForCtorParam("Items", opt => opt.MapFrom(s => s.Items ?? new List<OrderItem>()))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items ?? new List<OrderItem>()));

        // ReadModel -> DTO (lookups; mapeamento na Application, Infrastructure retorna apenas ReadModel)
        CreateMap<CustomerReadModel, CustomerLookupDto>();
        CreateMap<PaymentConditionReadModel, PaymentConditionLookupDto>();
    }
}