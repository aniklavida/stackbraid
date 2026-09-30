import { Component } from "@angular/core";
import { TranslocoPipe } from "@jsverse/transloco";

import { useRoles } from "../application/use-roles";

@Component({
  selector: "app-roles-page",
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: "./roles-page.html",
})
export class RolesPageComponent {
  protected readonly roles = useRoles();
}