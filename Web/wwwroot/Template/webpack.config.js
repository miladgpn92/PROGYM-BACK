const path = require("path");
const webpack = require("webpack");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CssMinimizerPlugin = require("css-minimizer-webpack-plugin");
const TerserPlugin = require("terser-webpack-plugin");
const CompressionPlugin = require("compression-webpack-plugin");
module.exports = {
    
    watch: true,

    mode: "production", //production | development
    stats: {
        errorDetails: true, // Enables detailed error messages
    },
    entry: {
        dist: [
            "./wwwroot/Shared/libs/jquery/dist/jquery.min.js",
            "./wwwroot/Template/assets/js/bootstrap.bundle.min.js",
            "./wwwroot/Template/assets/js/jquery.magnific-popup.min.js",
            "./wwwroot/Template/assets/js/swiper-bundle.min.js",
            "./wwwroot/Template/assets/js/counter.js",
            "./wwwroot/Template/assets/js/progressbar.js",
            "./wwwroot/Template/assets/js/gsap.min.js",
            "./wwwroot/Template/assets/js/ScrollSmoother.min.js",
            "./wwwroot/Template/assets/js/ScrollToPlugin.min.js",
            "./wwwroot/Template/assets/js/ScrollTrigger.min.js",
            "./wwwroot/Template/assets/js/SplitText.min.js",
            "./wwwroot/Template/assets/js/jquery.meanmenu.min.js",
            "./wwwroot/Template/assets/js/backToTop.js",
            "./wwwroot/Template/assets/js/main.js",
            "./wwwroot/Template/assets/js/error-handling.js",
            "./wwwroot/Template/assets/js/offcanvas.js",
            "./wwwroot/Template/assets/js/app.js",

            "./wwwroot/Shared/libs/jquery-validation/dist/jquery.validate.min.js",
            "./wwwroot/Shared/libs/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js",
        ],
        styles: [
            'shikwasa/dist/style.css',
            'lightgallery/css/lightgallery.css',
            'lightgallery/css/lg-zoom.css',
            'lightgallery/css/lg-thumbnail.css',
            "leaflet/dist/leaflet.css",
            "./wwwroot/Template/assets/css/bootstrap.min.css",
            "./wwwroot/Template/assets/css/all.min.css",
            "./wwwroot/Template/assets/css/swiper-bundle.min.css",
            "./wwwroot/Template/assets/css/progressbar.css",
            "./wwwroot/Template/assets/css/meanmenu.min.css",
            "./wwwroot/Template/assets/css/magnific-popup.css",

            "./wwwroot/Template/assets/css/style.scss",
            "./wwwroot/Template/assets/css/font.css",

        ],

    },

    resolve: {
        extensions: [".mjs", ".js", ".scss", ".css", ".ts"],
        fallback: {
            fs: false,
            path: false,
        },
    },

    module: {
        rules: [
            {
                test: /\.css$/i,
                use: [MiniCssExtractPlugin.loader, "css-loader"],
            },
            {
                test: /\.scss$/,
                use: [
                    MiniCssExtractPlugin.loader,
                    "css-loader",
                    "sass-loader",
                    {
                        loader: "postcss-loader",
                        options: {
                            postcssOptions: {
                                plugins: [require("tailwindcss"), require("autoprefixer")],
                            },
                        },
                    },
                ],
            },
            {
                test: /\.ts?$/,
                loader: "ts-loader",
                exclude: /node_modules|\.d\.ts$/,
            },
            {
                test: /\.d\.ts$/,
                loader: "ignore-loader",
            },
            {
                test: /\.(?:js|mjs|cjs)$/,
                exclude: /node_modules/,
                use: {
                    loader: "babel-loader",
                    options: {
                        presets: [["@babel/preset-env", { targets: "defaults" }]],
                    },
                },
            },
            {
                test: /\.(woff(2)?|ttf|eot)$/,
                type: "asset/resource",
                generator: {
                    filename: "./dist/assets/fonts/[name][hash][ext]",
                },
            },

            {
                test: /\.(svg|png|jpg|jpeg)$/,
                type: "asset/resource",
                generator: {
                    filename: "./dist/assets/images/[name][hash][ext]",
                },
            },

        ],
    },
    optimization: {
        minimizer: [new CssMinimizerPlugin(), new TerserPlugin()],
    },

    plugins: [
        new webpack.optimize.ModuleConcatenationPlugin({
            optimizationBailout: true,
        }),

        new MiniCssExtractPlugin({
            filename: "./dist/css/[name].css",
        }),
        new CompressionPlugin({
            algorithm: "gzip",
        }),
        new webpack.ProvidePlugin({
            $: "jquery",
            jQuery: "jquery",
            jquer: "jquery",
            "window.jquery": "jquery",
            process: "process/browser.js",

        }),
    ],
    ignoreWarnings: [
        {
            module: /module2\.js\?[34]/, // A RegExp
        },
        {
            module: /[13]/,
            message: /homepage/,
        },
        /warning from compiler/,
        (warning) => true,
    ],
    output: {
        filename: "dist/[name].js",
        path: path.resolve(__dirname),
    },
};
