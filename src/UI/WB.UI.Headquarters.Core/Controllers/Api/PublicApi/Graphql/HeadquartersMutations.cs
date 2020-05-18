﻿using HotChocolate.Types;
using Main.Core.Entities.SubEntities;
using WB.UI.Headquarters.Controllers.Api.PublicApi.Graphql.Maps;

namespace WB.UI.Headquarters.Controllers.Api.PublicApi.Graphql
{
    public class HeadquartersMutations : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field<MapsResolver>(t => t.DeleteMap(default))
                .Authorize(roles: new[] {nameof(UserRoles.Administrator), nameof(UserRoles.Headquarter)})
                .Type<Map>()
                .Argument("fileName", a => a.Description("Map file name").Type<NonNullType<StringType>>());

            descriptor.Field<MapsResolver>(t => t.DeleteUserFromMap(default, default))
                .Authorize(roles: new[] {nameof(UserRoles.Administrator), nameof(UserRoles.Headquarter), nameof(UserRoles.Supervisor),})
                .Type<Map>()
                .Argument("fileName", a => a.Description("Map file name").Type<NonNullType<StringType>>())
                .Argument("userName", a => a.Type<NonNullType<StringType>>());

            descriptor.Field<MapsResolver>(t => t.AddUserToMap(default, default))
                .Authorize(roles: new[] {nameof(UserRoles.Administrator), nameof(UserRoles.Headquarter), nameof(UserRoles.Supervisor),})
                .Type<Map>()
                .Argument("fileName", a => a.Description("Map file name").Type<NonNullType<StringType>>())
                .Argument("userName", a => a.Type<NonNullType<StringType>>());
        }
    }
}
