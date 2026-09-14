import 'package:flutter/widgets.dart';

import 'app.dart';
import 'app_dependencies.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(App(dependencies: AppDependencies()));
}
