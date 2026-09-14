import { Component } from "@angular/core";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { TranslocoPipe } from "@jsverse/transloco";

import { useRoles } from "../application/use-roles";

@Component({
  selector: "app-roles-page",
  standalone: true,
  imports: [MatProgressBarModule, TranslocoPipe],
  templateUrl: "./roles-page.html",
})
export class RolesPageComponent {
  protected readonly roles = useRoles();
}