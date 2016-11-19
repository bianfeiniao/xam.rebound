using System.Collections.Generic;
using System;
using Android.Content;
using Android.Views;
using Android.Widget;
using xam.rebound.core;
using Java.Text;
using Android.Graphics;
using Android.Util;
using Android.Content.Res;
namespace xam.rebound.android.ui
{
    /**
 * The SpringConfiguratorView provides a reusable view for live-editing all registered springs
 * within an Application. Each registered Spring can be accessed by its id and its tension and
 * friction properties can be edited while the user tests the effected UI live.
 */
    public class SpringConfiguratorView : FrameLayout
    {

        private static int MAX_SEEKBAR_VAL = 100000;
        private static float MIN_TENSION = 0;
        private static float MAX_TENSION = 200;
        private static float MIN_FRICTION = 0;
        private static float MAX_FRICTION = 50;
        private static DecimalFormat DECIMAL_FORMAT = new DecimalFormat("#.#");

        private SpinnerAdapter spinnerAdapter;
        private List<SpringConfig> mSpringConfigs = new List<SpringConfig>();
        private Spring mRevealerSpring;
        private float mStashPx;
        private float mRevealPx;
        private SpringConfigRegistry springConfigRegistry;
        private Color mTextColor = Color.Argb(255, 225, 225, 225);
        private SeekBar mTensionSeekBar;
        private SeekBar mFrictionSeekBar;
        private Spinner mSpringSelectorSpinner;
        private TextView mFrictionLabel;
        private TextView mTensionLabel;
        private SpringConfig mSelectedSpringConfig;

        public SpringConfiguratorView(Context context) : base(context, null) { }
        public SpringConfiguratorView(Context context, IAttributeSet attrs) : base(context, attrs, 0) { }
        // @TargetApi(Build.VERSION_CODES.HONEYCOMB)
        public SpringConfiguratorView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            SpringSystem springSystem = SpringSystem.create();
            springConfigRegistry = SpringConfigRegistry.getInstance();
            spinnerAdapter = new SpinnerAdapter(context);

            Resources resources = this.Resources;
            mRevealPx = Util.dpToPx(40, resources);
            mStashPx = Util.dpToPx(280, resources);

            mRevealerSpring = springSystem.createSpring();
            mRevealerSpring
                   .setCurrentValue(1)
                   .setEndValue(1)
                   .addListener(new SimpleSpringListener()
                   {
                       SpringUpdate = (spring) =>
                         {
                             float val = (float)spring.getCurrentValue();
                             float minTranslate = mRevealPx;
                             float maxTranslate = mStashPx;
                             float range = maxTranslate - minTranslate;
                             float yTranslate = (val * range) + minTranslate;
                             this.TranslationY=yTranslate;
                         }
                   });

            AddView(generateHierarchy(context));

            SeekbarListener seekbarListener = new SeekbarListener()
            {
                ProgressChanged = (seekBar, val, b) =>
                {
                    float tensionRange = MAX_TENSION - MIN_TENSION;
                    float frictionRange = MAX_FRICTION - MIN_FRICTION;
                    if (seekBar == mTensionSeekBar)
                    {
                        float scaledTension = ((val) * tensionRange) / MAX_SEEKBAR_VAL + MIN_TENSION;
                        mSelectedSpringConfig.tension =
                            OrigamiValueConverter.tensionFromOrigamiValue(scaledTension);
                        string roundedTensionLabel = DECIMAL_FORMAT.Format(scaledTension);
                        mTensionLabel.Text = "T:" + roundedTensionLabel;
                    }

                    if (seekBar == mFrictionSeekBar)
                    {
                        float scaledFriction = ((val) * frictionRange) / MAX_SEEKBAR_VAL + MIN_FRICTION;
                        mSelectedSpringConfig.friction =
                            OrigamiValueConverter.frictionFromOrigamiValue(scaledFriction);
                        string roundedFrictionLabel = DECIMAL_FORMAT.Format(scaledFriction);
                        mFrictionLabel.Text = "F:" + roundedFrictionLabel;
                    }
                }
            };

            mTensionSeekBar.Max = MAX_SEEKBAR_VAL;
            mTensionSeekBar.SetOnSeekBarChangeListener(seekbarListener);

            mFrictionSeekBar.Max = MAX_SEEKBAR_VAL;
            mFrictionSeekBar.SetOnSeekBarChangeListener(seekbarListener);

            mSpringSelectorSpinner.Adapter = spinnerAdapter;
            mSpringSelectorSpinner.OnItemSelectedListener = new SpringSelectedListener()
            {
                ItemSelected = (ad, v, i, l) =>
                {
                    mSelectedSpringConfig = mSpringConfigs[i];
                    updateSeekBarsForSpringConfig(mSelectedSpringConfig);
                }
            };
            refreshSpringConfigurations();
            this.TranslationY=mStashPx;
        }

        /**
         * Programmatically build up the view hierarchy to avoid the need for resources.
         * @return View hierarchy
         */
        private View generateHierarchy(Context context)
        {
            Resources resources = this.Resources;
            FrameLayout.LayoutParams _params;
            int fivePx = Util.dpToPx(5, resources);
            int tenPx = Util.dpToPx(10, resources);
            int twentyPx = Util.dpToPx(20, resources);
            TableLayout.LayoutParams tableLayoutParams = new TableLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent,
                1f);
            tableLayoutParams.SetMargins(0, 0, fivePx, 0);
            LinearLayout seekWrapper;

            FrameLayout root = new FrameLayout(context);
            _params = new LayoutParams(ViewGroup.LayoutParams.MatchParent, Util.dpToPx(300, resources));
            root.LayoutParameters = _params;

            FrameLayout container = new FrameLayout(context);
            _params = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
            _params.SetMargins(0, twentyPx, 0, 0);
            container.LayoutParameters = _params;
            container.SetBackgroundColor(Color.Argb(100, 0, 0, 0));
            root.AddView(container);

            mSpringSelectorSpinner = new Spinner(context, SpinnerMode.Dialog);
            _params = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent);
            _params.Gravity = GravityFlags.Top;
            _params.SetMargins(tenPx, tenPx, tenPx, 0);
            mSpringSelectorSpinner.LayoutParameters = _params;
            container.AddView(mSpringSelectorSpinner);

            LinearLayout linearLayout = new LinearLayout(context);

            _params = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent);
            _params.SetMargins(0, 0, 0, Util.dpToPx(80, resources));
            _params.Gravity = GravityFlags.Bottom;
            linearLayout.LayoutParameters = _params;
            linearLayout.Orientation = Android.Widget.Orientation.Vertical;
            container.AddView(linearLayout);
            seekWrapper = new LinearLayout(context);

            _params = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent);
            _params.SetMargins(tenPx, tenPx, tenPx, twentyPx);
            seekWrapper.SetPadding(tenPx, tenPx, tenPx, tenPx);
            seekWrapper.LayoutParameters = _params;
            seekWrapper.Orientation = Android.Widget.Orientation.Horizontal;
            linearLayout.AddView(seekWrapper);

            mTensionSeekBar = new SeekBar(context);
            mTensionSeekBar.LayoutParameters = tableLayoutParams;
            seekWrapper.AddView(mTensionSeekBar);

            mTensionLabel = new TextView(Context);
            mTensionLabel.SetTextColor(mTextColor);

            _params = new LayoutParams(
                Util.dpToPx(50, resources),
                ViewGroup.LayoutParams.MatchParent);
            mTensionLabel.Gravity = GravityFlags.CenterVertical | GravityFlags.Left;
            mTensionLabel.LayoutParameters = _params;
            mTensionLabel.SetMaxLines(1);
            seekWrapper.AddView(mTensionLabel);

            seekWrapper = new LinearLayout(context);
            _params = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent);
            _params.SetMargins(tenPx, tenPx, tenPx, twentyPx);
            seekWrapper.SetPadding(tenPx, tenPx, tenPx, tenPx);
            seekWrapper.LayoutParameters = _params;
            seekWrapper.Orientation = Android.Widget.Orientation.Horizontal;
            linearLayout.AddView(seekWrapper);

            mFrictionSeekBar = new SeekBar(context);
            mFrictionSeekBar.LayoutParameters = tableLayoutParams;
            seekWrapper.AddView(mFrictionSeekBar);

            mFrictionLabel = new TextView(Context);
            mFrictionLabel.SetTextColor(mTextColor);
            _params = new LayoutParams(Util.dpToPx(50, resources), ViewGroup.LayoutParams.MatchParent);
            mFrictionLabel.Gravity = GravityFlags.CenterVertical | GravityFlags.Left;
            mFrictionLabel.LayoutParameters = _params;
            mFrictionLabel.SetMaxLines(1);
            seekWrapper.AddView(mFrictionLabel);

            View nub = new View(context);
            _params = new LayoutParams(Util.dpToPx(60, resources), Util.dpToPx(40, resources));
            _params.Gravity = GravityFlags.Top | GravityFlags.Center;
            nub.LayoutParameters = _params;

            nub.SetOnTouchListener(new OnNubTouchListener()
            {
                Touch = (View, motionEvent) =>
                {
                    if (motionEvent.Action == MotionEventActions.Down)
                    {
                        togglePosition();
                    }
                    return true;
                }
            });
            nub.SetBackgroundColor(Color.Argb(255, 0, 164, 209));
            root.AddView(nub);
            return root;
        }

        /**
         * remove the configurator from its parent and clean up springs and listeners
         */
        public void destroy()
        {
            ViewGroup parent = (ViewGroup)this.Parent;
            if (parent != null)
            {
                parent.RemoveView(this);
            }
            mRevealerSpring.destroy();
        }

        /**
         * reload the springs from the registry and update the UI
         */
        public void refreshSpringConfigurations()
        {
            Dictionary<SpringConfig, string> springConfigMap = springConfigRegistry.getAllSpringConfig();

            spinnerAdapter.clear();
            mSpringConfigs.Clear();

            foreach (var entry in springConfigMap.Keys)
            {
                if (entry == SpringConfig.defaultConfig)
                {
                    continue;
                }
                mSpringConfigs.Add(entry);
                spinnerAdapter.add(springConfigMap[entry]);
            }
            // Add the default config in last.
            mSpringConfigs.Add(SpringConfig.defaultConfig);
            spinnerAdapter.add(springConfigMap[SpringConfig.defaultConfig]);
            spinnerAdapter.NotifyDataSetChanged();
            if (mSpringConfigs.Count > 0)
            {
                mSpringSelectorSpinner.SetSelection(0);
            }
        }

        public class SpringSelectedListener : Java.Lang.Object, AdapterView.IOnItemSelectedListener
        {
            public Action<AdapterView, View, int, long> ItemSelected { get; set; }
            public Action<AdapterView> NothingSelected { get; set; }
            ////@Override
            public void OnItemSelected(AdapterView adapterView, View view, int i, long l)
            {
                ItemSelected?.Invoke(adapterView, view, i, l);
            }
            ////@Override
            public void OnNothingSelected(AdapterView adapterView)
            {
                NothingSelected?.Invoke(adapterView);
            }
        }

        /**
         * listen to events on seekbars and update registered springs accordingly
         */
        public class SeekbarListener : Java.Lang.Object, SeekBar.IOnSeekBarChangeListener
        {
            public Action<SeekBar, int, bool> ProgressChanged { get; set; }
            public Action<SeekBar> StartTrackingTouch { get; set; }
            public Action<SeekBar> StopTrackingTouch { get; set; }

            public void OnProgressChanged(SeekBar seekBar, int val, bool b)
            {
                ProgressChanged?.Invoke(seekBar, val, b);
            }

            public void OnStartTrackingTouch(SeekBar seekBar)
            {
                StartTrackingTouch?.Invoke(seekBar);
            }

            ////@Override
            public void OnStopTrackingTouch(SeekBar seekBar)
            {
                StopTrackingTouch?.Invoke(seekBar);
            }
        }

        /**
         * update the position of the seekbars based on the spring value;
         * @param springConfig current editing spring
         */
        private void updateSeekBarsForSpringConfig(SpringConfig springConfig)
        {
            float tension = (float)OrigamiValueConverter.origamiValueFromTension(springConfig.tension);
            float tensionRange = MAX_TENSION - MIN_TENSION;
            int scaledTension = Java.Lang.Math.Round(((tension - MIN_TENSION) * MAX_SEEKBAR_VAL) / tensionRange);

            float friction = (float)OrigamiValueConverter.origamiValueFromFriction(springConfig.friction);
            float frictionRange = MAX_FRICTION - MIN_FRICTION;
            int scaledFriction = Java.Lang.Math.Round(((friction - MIN_FRICTION) * MAX_SEEKBAR_VAL) / frictionRange);

            mTensionSeekBar.Progress = scaledTension;
            mFrictionSeekBar.Progress = scaledFriction;
        }

        /**
         * toggle visibility when the nub is tapped.
         */
        public class OnNubTouchListener : Java.Lang.Object, View.IOnTouchListener
        {

            public Func<View, MotionEvent, bool> Touch { get; set; }
            public bool OnTouch(View view, MotionEvent motionEvent)
            {
                return Touch(view, motionEvent);
            }
        }

        private void togglePosition()
        {
            double currentValue = mRevealerSpring.getEndValue();
            mRevealerSpring
                .setEndValue(currentValue == 1 ? 0 : 1);
        }

        public class SpinnerAdapter : BaseAdapter
        {
            private Context mContext;
            private List<string> mStrings;
            private Color SmTextColor;

            public override int Count
            {
                get
                {
                    return mStrings.Count;
                }
            }

            public SpinnerAdapter(Context context, string smTextColor = "#FFFFFF")
            {
                mContext = context;
                mStrings = new List<string>();
                SmTextColor = Color.ParseColor(smTextColor);
            }

            public void add(string _string)
            {
                mStrings.Add(_string);
                NotifyDataSetChanged();
            }

            /**
             * Remove all elements from the list.
             */
            public void clear()
            {
                mStrings.Clear();
                NotifyDataSetChanged();
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                TextView textView;
                if (convertView == null)
                {
                    textView = new TextView(mContext);
                    AbsListView.LayoutParams _params = new AbsListView.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.MatchParent);
                    textView.LayoutParameters = _params;
                    // int twelvePx = Util.dpToPx(12, getResources());
                    int twelvePx = 14;
                    textView.SetPadding(twelvePx, twelvePx, twelvePx, twelvePx);
                    textView.SetTextColor(SmTextColor);
                }
                else
                {
                    textView = (TextView)convertView;
                }
                textView.Text = mStrings[position];
                return textView;
            }
            public override Java.Lang.Object GetItem(int position)
            {
                return mStrings[position];
            }

            public override long GetItemId(int position)
            {
                return position;
            }
        }




}
}