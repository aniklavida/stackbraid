import { Component, inject } from "@angular/core";
import { RouterLink } from "@angular/router";
import { TranslocoPipe } from "@jsverse/transloco";

import { AuthService } from "../../../../shared/auth/auth.service";

@Component({
  selector: "app-home-page",
  standalone: true,
  imports: [RouterLink, TranslocoPipe],
  templateUrl: "./home-page.html",
})
export class HomePageComponent {
  protected readonly auth = inject(AuthService);
}
